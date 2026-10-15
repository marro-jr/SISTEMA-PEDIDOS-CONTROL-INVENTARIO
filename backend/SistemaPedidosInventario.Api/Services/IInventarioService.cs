using SistemaPedidosInventario.Api.DTOs;

namespace SistemaPedidosInventario.Api.Services;

public interface IInventarioService
{
    Task<IEnumerable<MovimientoStockDto>> ObtenerMovimientosAsync(int? productoId = null, int limite = 100);
    Task<IEnumerable<ProductoBajoStockDto>> ObtenerProductosBajoStockAsync(int umbral = 10);
}
