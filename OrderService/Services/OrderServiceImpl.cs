using Shared.Common.DTOs;
using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Order;
using Shared.Common.Exceptions;
using Shared.Common.Interfaces;
using Shared.Common.Interfaces.Repositories;
using Shared.Common.Interfaces.Services;
using Shared.Common.Models;
using Shared.Infrastructure.Logging;

namespace OrderService.Services;

public class OrderServiceImpl : IOrderService
{
    private readonly IOrderRepository _orderRepo;
    private readonly IProductRepository _productRepo;
    private readonly IAuditLogService _auditLog;
    private readonly ILogger<OrderServiceImpl> _logger;

    public OrderServiceImpl(
        IOrderRepository orderRepo, IProductRepository productRepo,
        IAuditLogService auditLog, ILogger<OrderServiceImpl> logger)
    {
        _orderRepo = orderRepo;
        _productRepo = productRepo;
        _auditLog = auditLog;
        _logger = logger;
    }

    public async Task<ApiResponse<OrderResponse>> GetByIdAsync(int id, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);

        // ป้องกันดูออเดอร์คนอื่น (IDOR Protection)
        if (order.UserId != userId)
            throw new ForbiddenException("You can only view your own orders.");

        var items = await _orderRepo.GetOrderItemsAsync(id);
        return ApiResponse<OrderResponse>.SuccessResponse(await MapToResponse(order, items));
    }

    public async Task<ApiResponse<PaginatedResponse<OrderResponse>>> GetByUserIdAsync(int userId, PaginationRequest request)
    {
        var orders = await _orderRepo.GetByUserIdAsync(userId);
        var responses = new List<OrderResponse>();
        foreach (var order in orders)
        {
            var items = await _orderRepo.GetOrderItemsAsync(order.Id);
            responses.Add(await MapToResponse(order, items));
        }

        var paginated = new PaginatedResponse<OrderResponse>
        {
            Items = responses.Skip((request.PageNumber - 1) * request.PageSize).Take(request.PageSize).ToList(),
            TotalCount = responses.Count,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize
        };
        return ApiResponse<PaginatedResponse<OrderResponse>>.SuccessResponse(paginated);
    }

    public async Task<ApiResponse<OrderResponse>> CreateAsync(CreateOrderRequest request, int userId)
    {
        // Validate stock availability and calculate totals
        decimal totalAmount = 0;
        var orderItems = new List<(OrderItem item, Product product)>();

        foreach (var itemReq in request.Items)
        {
            var product = await _productRepo.GetByIdAsync(itemReq.ProductId)
                ?? throw new NotFoundException("Product", itemReq.ProductId);

            if (product.StockQuantity < itemReq.Quantity)
                throw new AppException($"Insufficient stock for '{product.Name}'. Available: {product.StockQuantity}", 400);

            var itemTotal = product.Price * itemReq.Quantity;
            totalAmount += itemTotal;

            orderItems.Add((new OrderItem
            {
                ProductId = itemReq.ProductId,
                Quantity = itemReq.Quantity,
                UnitPrice = product.Price,
                TotalPrice = itemTotal,
                CreatedBy = userId.ToString()
            }, product));
        }

        // Create order
        var order = new Order
        {
            UserId = userId,
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..8].ToUpper()}",
            TotalAmount = totalAmount,
            Status = "Pending",
            ShippingAddress = request.ShippingAddress,
            CreatedBy = userId.ToString()
        };

        order.Id = await _orderRepo.CreateAsync(order);

        // Create order items and update stock
        foreach (var (item, product) in orderItems)
        {
            item.OrderId = order.Id;
            await _orderRepo.CreateOrderItemAsync(item);
            await _productRepo.UpdateStockAsync(product.Id, -item.Quantity);
        }

        await _auditLog.LogAsync(new AuditLogEntry
        {
            UserId = userId,
            Action = "CreateOrder",
            EntityType = "Order",
            EntityId = order.Id.ToString(),
            NewValues = request,
            ServiceName = "OrderService"
        });

        _logger.LogInformation("Order {OrderNumber} created by user {UserId}, total: {Total}",
            order.OrderNumber, userId, totalAmount);

        var items = await _orderRepo.GetOrderItemsAsync(order.Id);
        return ApiResponse<OrderResponse>.SuccessResponse(await MapToResponse(order, items), "Order created");
    }

    public async Task<ApiResponse<OrderResponse>> UpdateStatusAsync(int id, UpdateOrderStatusRequest request)
    {
        var order = await _orderRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);

        var validTransitions = new Dictionary<string, string[]>
        {
            ["Pending"] = new[] { "Confirmed", "Cancelled" },
            ["Confirmed"] = new[] { "Shipped", "Cancelled" },
            ["Shipped"] = new[] { "Delivered" },
            ["Delivered"] = Array.Empty<string>(),
            ["Cancelled"] = Array.Empty<string>()
        };

        if (!validTransitions.ContainsKey(order.Status) ||
            !validTransitions[order.Status].Contains(request.Status))
            throw new AppException($"Cannot transition from '{order.Status}' to '{request.Status}'");

        var oldStatus = order.Status;
        await _orderRepo.UpdateStatusAsync(id, request.Status);
        order.Status = request.Status;

        await _auditLog.LogAsync(new AuditLogEntry
        {
            Action = "UpdateOrderStatus",
            EntityType = "Order",
            EntityId = id.ToString(),
            OldValues = new { Status = oldStatus },
            NewValues = new { Status = request.Status },
            ServiceName = "OrderService"
        });

        var items = await _orderRepo.GetOrderItemsAsync(id);
        return ApiResponse<OrderResponse>.SuccessResponse(await MapToResponse(order, items));
    }

    public async Task<ApiResponse<bool>> CancelOrderAsync(int id, int userId)
    {
        var order = await _orderRepo.GetByIdAsync(id)
            ?? throw new NotFoundException("Order", id);

        if (order.UserId != userId)
            throw new ForbiddenException("You can only cancel your own orders.");

        if (order.Status != "Pending" && order.Status != "Confirmed")
            throw new AppException($"Cannot cancel order in '{order.Status}' status");

        await _orderRepo.UpdateStatusAsync(id, "Cancelled");

        // Restore stock
        var items = await _orderRepo.GetOrderItemsAsync(id);
        foreach (var item in items)
            await _productRepo.UpdateStockAsync(item.ProductId, item.Quantity);

        await _auditLog.LogAsync(new AuditLogEntry
        {
            UserId = userId,
            Action = "CancelOrder",
            EntityType = "Order",
            EntityId = id.ToString(),
            ServiceName = "OrderService"
        });

        return ApiResponse<bool>.SuccessResponse(true, "Order cancelled and stock restored");
    }

    private async Task<OrderResponse> MapToResponse(Order order, IEnumerable<OrderItem> items)
    {
        var itemResponses = new List<OrderItemResponse>();
        foreach (var item in items)
        {
            var product = await _productRepo.GetByIdAsync(item.ProductId);
            itemResponses.Add(new OrderItemResponse(
                item.ProductId, product?.Name ?? "Unknown",
                item.Quantity, item.UnitPrice, item.TotalPrice));
        }

        return new OrderResponse(
            order.Id, order.OrderNumber, order.TotalAmount,
            order.Status, order.ShippingAddress, order.CreatedAt, itemResponses);
    }
}
