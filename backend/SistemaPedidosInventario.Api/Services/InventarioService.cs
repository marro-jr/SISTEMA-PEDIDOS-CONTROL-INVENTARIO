using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Exceptions;

namespace SistemaPedidosInventario.Api.Services;

public class InventarioService : IInventarioService
{
    private readonly ApplicationDbContext _contexto;

    public InventarioService(ApplicationDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IEnumerable<MovimientoStockDto>> ObtenerMovimientosAsync(int? productoId = null, int limite = 100)
    {
        var consulta = _contexto.StockMovements
            .AsNoTracking()
            .Include(sm => sm.Product)
            .AsQueryable();

        if (productoId.HasValue)
        {
            consulta = consulta.Where(sm => sm.ProductId == productoId.Value);
        }

        return await consulta
            .OrderByDescending(sm => sm.CreatedAt)
            .Take(limite)
            .Select(sm => new MovimientoStockDto
            {
                Id = sm.Id,
                ProductId = sm.ProductId,
                ProductName = sm.Product != null ? sm.Product.Name : string.Empty,
                Quantity = sm.Quantity,
                MovementType = sm.MovementType,
                CreatedAt = sm.CreatedAt,
                OrderId = sm.OrderId
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<ProductoBajoStockDto>> ObtenerProductosBajoStockAsync(int umbral = 10)
    {
        if (umbral < 0)
        {
            throw new BusinessRuleException("El umbral de stock no puede ser un valor negativo.");
        }

        return await _contexto.Products
            .AsNoTracking()
            .Where(p => p.Active && p.Stock <= umbral)
            .OrderBy(p => p.Stock)
            .Select(p => new ProductoBajoStockDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price,
                Stock = p.Stock,
                Active = p.Active,
                Umbral = umbral
            })
            .ToListAsync();
    }
}
