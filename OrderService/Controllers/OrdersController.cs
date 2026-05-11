using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs;
using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Order;
using Shared.Common.Interfaces;
using Shared.Common.Interfaces.Services;
using Shared.Common.Validators;
using Shared.Common.Validators.Order;
using System.Security.Claims;

namespace OrderService.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public OrdersController(IOrderService orderService) => _orderService = orderService;

    [HttpGet]
    public async Task<IActionResult> GetMyOrders([FromQuery] PaginationRequest request)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _orderService.GetByUserIdAsync(userId, request));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _orderService.GetByIdAsync(id, userId));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest request)
    {
        var validator = new CreateOrderRequestValidator();
        var validation = await validator.ValidateAsync(request);
        if (!validation.IsValid)
            return UnprocessableEntity(ApiResponse<object>.FailResponse(
                "Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));

        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _orderService.CreateAsync(request, userId);
        return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        => Ok(await _orderService.UpdateStatusAsync(id, request));

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        return Ok(await _orderService.CancelOrderAsync(id, userId));
    }

    [HttpGet("/health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { Status = "Healthy", Service = "OrderService", Timestamp = DateTime.UtcNow });
}
