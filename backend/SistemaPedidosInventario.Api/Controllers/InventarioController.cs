using Microsoft.AspNetCore.Mvc;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Services;

namespace SistemaPedidosInventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InventarioController : ControllerBase
{
    private readonly IInventarioService _inventarioService;

    public InventarioController(IInventarioService inventarioService)
    {
        _inventarioService = inventarioService;
    }

    [HttpGet("movimientos")]
    [ProducesResponseType(typeof(IEnumerable<MovimientoStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerMovimientos([FromQuery] int? productoId, [FromQuery] int limite = 100)
    {
        var movimientos = await _inventarioService.ObtenerMovimientosAsync(productoId, limite);
        return Ok(movimientos);
    }

    [HttpGet("bajo-stock")]
    [ProducesResponseType(typeof(IEnumerable<ProductoBajoStockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerProductosBajoStock([FromQuery] int umbral = 10)
    {
        var productos = await _inventarioService.ObtenerProductosBajoStockAsync(umbral);
        return Ok(productos);
    }
}
