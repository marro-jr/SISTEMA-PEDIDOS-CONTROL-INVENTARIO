using Microsoft.AspNetCore.Mvc;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Services;

namespace SistemaPedidosInventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductosController : ControllerBase
{
    private readonly IProductosService _productosService;

    public ProductosController(IProductosService productosService)
    {
        _productosService = productosService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerProductos([FromQuery] bool? soloActivos)
    {
        var productos = await _productosService.ObtenerTodosAsync(soloActivos);
        return Ok(productos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerProductoPorId(int id)
    {
        var producto = await _productosService.ObtenerPorIdAsync(id);
        return Ok(producto);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CrearProducto([FromBody] CrearProductoDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var nuevoProducto = await _productosService.CrearAsync(dto);
        return CreatedAtAction(nameof(ObtenerProductoPorId), new { id = nuevoProducto.Id }, nuevoProducto);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ActualizarProducto(int id, [FromBody] ActualizarProductoDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var productoActualizado = await _productosService.ActualizarAsync(id, dto);
        return Ok(productoActualizado);
    }

    [HttpPatch("{id:int}/inactivar")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InactivarProducto(int id)
    {
        await _productosService.InactivarAsync(id);
        return NoContent();
    }
}
