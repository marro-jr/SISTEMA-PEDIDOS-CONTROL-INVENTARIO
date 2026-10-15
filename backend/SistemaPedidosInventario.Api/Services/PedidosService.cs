using System.Data;
using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;

namespace SistemaPedidosInventario.Api.Services;

public class PedidosService : IPedidosService
{
    private readonly ApplicationDbContext _contexto;

    public PedidosService(ApplicationDbContext contexto)
    {
        _contexto = contexto;
    }

    public async Task<IEnumerable<PedidoDto>> ObtenerPedidosAsync()
    {
        return await _contexto.Orders
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new PedidoDto
            {
                Id = o.Id,
                CreatedAt = o.CreatedAt,
                Total = o.Total,
                Status = o.Status,
                Detalle = o.OrderDetails.Select(d => new DetallePedidoDto
                {
                    Id = d.Id,
                    ProductId = d.ProductId,
                    ProductName = d.Product != null ? d.Product.Name : string.Empty,
                    Quantity = d.Quantity,
                    UnitPrice = d.UnitPrice,
                    Subtotal = d.Subtotal
                }).ToList()
            })
            .ToListAsync();
    }

    public async Task<PedidoDto> ObtenerPorIdAsync(int id)
    {
        var pedido = await _contexto.Orders
            .AsNoTracking()
            .Include(o => o.OrderDetails)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (pedido == null)
        {
            throw new NotFoundException($"El pedido con ID {id} no fue encontrado.");
        }

        return new PedidoDto
        {
            Id = pedido.Id,
            CreatedAt = pedido.CreatedAt,
            Total = pedido.Total,
            Status = pedido.Status,
            Detalle = pedido.OrderDetails.Select(d => new DetallePedidoDto
            {
                Id = d.Id,
                ProductId = d.ProductId,
                ProductName = d.Product != null ? d.Product.Name : string.Empty,
                Quantity = d.Quantity,
                UnitPrice = d.UnitPrice,
                Subtotal = d.Subtotal
            }).ToList()
        };
    }

    public async Task<PedidoDto> RegistrarPedidoAsync(CrearPedidoDto dto)
    {
        if (dto.Items == null || dto.Items.Count == 0)
        {
            throw new BusinessRuleException("El pedido debe contener al menos un producto.");
        }

        // Agrupar cantidades por producto si se repite en la solicitud
        var cantidadesSolicitadas = dto.Items
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                CantidadTotal = g.Sum(x => x.Quantity)
            })
            .ToList();

        // Validar que ninguna cantidad sea menor o igual a cero
        foreach (var item in cantidadesSolicitadas)
        {
            if (item.CantidadTotal <= 0)
            {
                throw new BusinessRuleException($"La cantidad para el producto ID {item.ProductId} debe ser mayor a cero.");
            }
        }

        // Manejo transaccional para atomicidad de pedido, detalles, descuento y movimientos
        var estrategia = _contexto.Database.CreateExecutionStrategy();

        return await estrategia.ExecuteAsync(async () =>
        {
            using var transaccion = await _contexto.Database.BeginTransactionAsync(IsolationLevel.RepeatableRead);
            try
            {
                var idsProductos = cantidadesSolicitadas.Select(i => i.ProductId).ToList();
                var productos = await _contexto.Products
                    .Where(p => idsProductos.Contains(p.Id))
                    .ToListAsync();

                // Validar que todos los productos existan en catálogo
                foreach (var item in cantidadesSolicitadas)
                {
                    var producto = productos.FirstOrDefault(p => p.Id == item.ProductId);
                    if (producto == null)
                    {
                        throw new NotFoundException($"El producto con ID {item.ProductId} no existe.");
                    }

                    // Regla de negocio: no vender productos inactivos
                    if (!producto.Active)
                    {
                        throw new BusinessRuleException($"El producto '{producto.Name}' está inactivo y no puede venderse.");
                    }

                    // Regla de negocio: no vender más unidades que las disponibles en stock
                    if (producto.Stock < item.CantidadTotal)
                    {
                        throw new BusinessRuleException(
                            $"Stock insuficiente para '{producto.Name}'. Disponible: {producto.Stock}, Solicitado: {item.CantidadTotal}.");
                    }
                }

                var nuevoPedido = new Order
                {
                    CreatedAt = DateTime.UtcNow,
                    Status = "Completado",
                    Total = 0m
                };

                decimal totalCalculado = 0m;
                var detalles = new List<OrderDetail>();
                var movimientos = new List<StockMovement>();

                foreach (var item in cantidadesSolicitadas)
                {
                    var producto = productos.First(p => p.Id == item.ProductId);

                    // Congelar precio unitario histórico y calcular subtotales en el servidor
                    decimal precioUnitario = producto.Price;
                    decimal subtotal = precioUnitario * item.CantidadTotal;
                    totalCalculado += subtotal;

                    // Descuento atómico de existencias
                    producto.Stock -= item.CantidadTotal;

                    detalles.Add(new OrderDetail
                    {
                        Order = nuevoPedido,
                        ProductId = producto.Id,
                        Quantity = item.CantidadTotal,
                        UnitPrice = precioUnitario,
                        Subtotal = subtotal
                    });

                    // Auditoría obligatoria en movimientos de inventario
                    movimientos.Add(new StockMovement
                    {
                        ProductId = producto.Id,
                        Quantity = -item.CantidadTotal,
                        MovementType = "VENTA_PEDIDO",
                        CreatedAt = nuevoPedido.CreatedAt,
                        Order = nuevoPedido
                    });
                }

                nuevoPedido.Total = totalCalculado;
                nuevoPedido.OrderDetails = detalles;

                await _contexto.Orders.AddAsync(nuevoPedido);
                await _contexto.StockMovements.AddRangeAsync(movimientos);

                // Concurrencia optimista de EF Core lanzará DbUpdateConcurrencyException si otro pedido modificó el stock simultáneamente
                await _contexto.SaveChangesAsync();
                await transaccion.CommitAsync();

                return new PedidoDto
                {
                    Id = nuevoPedido.Id,
                    CreatedAt = nuevoPedido.CreatedAt,
                    Total = nuevoPedido.Total,
                    Status = nuevoPedido.Status,
                    Detalle = detalles.Select(d => new DetallePedidoDto
                    {
                        Id = d.Id,
                        ProductId = d.ProductId,
                        ProductName = productos.First(p => p.Id == d.ProductId).Name,
                        Quantity = d.Quantity,
                        UnitPrice = d.UnitPrice,
                        Subtotal = d.Subtotal
                    }).ToList()
                };
            }
            catch
            {
                // Reversión total garantizada si cualquier paso falla
                await transaccion.RollbackAsync();
                throw;
            }
        });
    }
}
