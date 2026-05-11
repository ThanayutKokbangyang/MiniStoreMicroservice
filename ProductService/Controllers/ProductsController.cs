
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Common.DTOs.Common;
using Shared.Common.DTOs.Product;
using Shared.Common.Interfaces.Services;
using Shared.Common.Validators.Product;
using System.Security.Claims;

namespace ProductService.Controllers
{
    [ApiController]
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiVersion("1.0")]
    [Authorize]
    [Produces("application/json")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;

        public ProductsController(IProductService productService) => _productService = productService;

        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] PaginationRequest request) => Ok(await _productService.GetAllAsync(request));

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id) => Ok(await _productService.GetByIdAsync(id));

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
        {
            var validator = new CreateProductRequestValidator();
            var validation = await validator.ValidateAsync(request);

            if (!validation.IsValid)
                return UnprocessableEntity(ApiResponse<object>.FailResponse("Validation failed", validation.Errors.Select(e => e.ErrorMessage).ToList()));

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            
            var result = await _productService.CreateAsync(request, userId);

            return CreatedAtAction(nameof(GetById), new { id = result.Data!.Id }, result);

        }

        [HttpPut("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateProductRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            return Ok(await _productService.UpdateAsync(id, request, userId));
        }

        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id) => Ok(await _productService.DeleteAsync(id));

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] PaginationRequest request) => Ok(await _productService.SearchAsync(term, request));

        [HttpGet("/health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { Status = "Healthy", Service = "ProductService", Timestamp = DateTime.UtcNow });

    }
}
