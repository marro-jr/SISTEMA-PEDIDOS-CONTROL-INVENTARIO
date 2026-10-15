using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;

namespace SistemaPedidosInventario.Api.Services;

public class ProductosService : IProductosService
{
    private readonly ApplicationDbContext _contexto;

    public ProductosService(ApplicationDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IEnumerable<ProductoDto>> ObtenerTodosAsync(bool? soloActivos = null)
    {
        var consulta = _contexto.Products.AsNoTracking();

        if (soloActivos.HasValue && soloActivos.Value)
        {
            consulta = consulta.Where(p => p.Active);
        }

        return await consulta
            .OrderBy(p => p.Name)
            .Select(p => new ProductoDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Price = p.Price,
                Stock = p.Stock,
                Active = p.Active
            })
            .ToListAsync();
    }

    public async Task<ProductoDto> ObtenerPorIdAsync(int id)
    {
        var producto = await _contexto.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);

        if (producto == null)
        {
            throw new NotFoundException($"El producto con ID {id} no fue encontrado.");
        }

        return new ProductoDto
        {
            Id = producto.Id,
            Name = producto.Name,
            Description = producto.Description,
            Price = producto.Price,
            Stock = producto.Stock,
            Active = producto.Active
        };
    }

    public async Task<ProductoDto> CrearAsync(CrearProductoDto dto)
    {
        // Regla: no registrar productos con precio menor o igual a cero
        if (dto.Price <= 0)
        {
            throw new BusinessRuleException("El precio del producto debe ser mayor a cero.");
        }

        // Regla: no permitir existencias negativas iniciales
        if (dto.Stock < 0)
        {
            throw new BusinessRuleException("El stock inicial no puede ser un valor negativo.");
        }

        var producto = new Product
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Price = dto.Price,
            Stock = dto.Stock,
            Active = true
        };

        _contexto.Products.Add(producto);
        await _contexto.SaveChangesAsync();

        // Registrar movimiento inicial de inventario si tiene existencias
        if (producto.Stock > 0)
        {
            _contexto.StockMovements.Add(new StockMovement
            {
                ProductId = producto.Id,
                Quantity = producto.Stock,
                MovementType = "INICIAL",
                CreatedAt = DateTime.UtcNow,
                OrderId = null
            });
            await _contexto.SaveChangesAsync();
        }

        return new ProductoDto
        {
            Id = producto.Id,
            Name = producto.Name,
            Description = producto.Description,
            Price = producto.Price,
            Stock = producto.Stock,
            Active = producto.Active
        };
    }

    public async Task<ProductoDto> ActualizarAsync(int id, ActualizarProductoDto dto)
    {
        var producto = await _contexto.Products.FindAsync(id);
        if (producto == null)
        {
            throw new NotFoundException($"El producto con ID {id} no fue encontrado.");
        }

        if (dto.Price <= 0)
        {
            throw new BusinessRuleException("El precio del producto debe ser mayor a cero.");
        }

        if (dto.Stock < 0)
        {
            throw new BusinessRuleException("El stock del producto no puede ser negativo.");
        }

        // Si el stock varió manualmente por ajuste, registrar movimiento de auditoría
        int diferenciaStock = dto.Stock - producto.Stock;
        if (diferenciaStock != 0)
        {
            _contexto.StockMovements.Add(new StockMovement
            {
                ProductId = producto.Id,
                Quantity = diferenciaStock,
                MovementType = "AJUSTE",
                CreatedAt = DateTime.UtcNow,
                OrderId = null
            });
        }

        producto.Name = dto.Name.Trim();
        producto.Description = dto.Description?.Trim();
        producto.Price = dto.Price;
        producto.Stock = dto.Stock;
        producto.Active = dto.Active;

        await _contexto.SaveChangesAsync();

        return new ProductoDto
        {
            Id = producto.Id,
            Name = producto.Name,
            Description = producto.Description,
            Price = producto.Price,
            Stock = producto.Stock,
            Active = producto.Active
        };
    }

    public async Task<bool> InactivarAsync(int id)
    {
        var producto = await _contexto.Products.FindAsync(id);
        if (producto == null)
        {
            throw new NotFoundException($"El producto con ID {id} no fue encontrado.");
        }

        // Baja lógica recomendada para mantener integridad histórica
        producto.Active = false;
        await _contexto.SaveChangesAsync();
        return true;
    }
}
