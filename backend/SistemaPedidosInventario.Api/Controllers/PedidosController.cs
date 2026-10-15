using Microsoft.AspNetCore.Mvc;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Services;

namespace SistemaPedidosInventario.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidosController : ControllerBase
{
    private readonly IPedidosService _pedidosService;

    public PedidosController(IPedidosService pedidosService)
    {
        _pedidosService = pedidosService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PedidoDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ObtenerPedidos()
    {
        var pedidos = await _pedidosService.ObtenerPedidosAsync();
        return Ok(pedidos);
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ObtenerPedidoPorId(int id)
    {
        var pedido = await _pedidosService.ObtenerPorIdAsync(id);
        return Ok(pedido);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PedidoDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RegistrarPedido([FromBody] CrearPedidoDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var pedidoRegistrado = await _pedidosService.RegistrarPedidoAsync(dto);
        return CreatedAtAction(nameof(ObtenerPedidoPorId), new { id = pedidoRegistrado.Id }, pedidoRegistrado);
    }
}
