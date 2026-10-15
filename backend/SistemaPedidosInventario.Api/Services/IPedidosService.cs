using SistemaPedidosInventario.Api.DTOs;

namespace SistemaPedidosInventario.Api.Services;

public interface IPedidosService
{
    Task<IEnumerable<PedidoDto>> ObtenerPedidosAsync();
    Task<PedidoDto> ObtenerPorIdAsync(int id);
    Task<PedidoDto> RegistrarPedidoAsync(CrearPedidoDto dto);
}
