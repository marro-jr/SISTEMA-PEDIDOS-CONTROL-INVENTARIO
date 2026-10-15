using SistemaPedidosInventario.Api.DTOs;

namespace SistemaPedidosInventario.Api.Services;

public interface IProductosService
{
    Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(bool? soloActivos = null);
    Task<ProductoDto> ObtenerPorIdAsync(int id);
    Task<ProductoDto> CrearAsync(CrearProductoDto dto);
    Task<ProductoDto> ActualizarAsync(int id, ActualizarProductoDto dto);
    Task<bool> InactivarAsync(int id);
}
