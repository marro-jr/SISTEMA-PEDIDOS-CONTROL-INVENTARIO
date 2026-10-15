using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;
using SistemaPedidosInventario.Api.Services;
using Xunit;

namespace SistemaPedidosInventario.Tests;

public class PedidosServiceTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<ApplicationDbContext> _opcionesContexto;

    public PedidosServiceTests()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        _opcionesContexto = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = CrearContexto();
        contexto.Database.EnsureCreated();
    }

    private ApplicationDbContext CrearContexto() => new(_opcionesContexto);

    public void Dispose()
    {
        _conexion.Dispose();
    }

    [Fact]
    public async Task RegistrarPedido_ConDatosValidos_DescuentaStockYCalculaTotalCorrectamente()
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.Add(new Product
            {
                Id = 1,
                Name = "Teclado Mecánico",
                Price = 150.00m,
                Stock = 10,
                Active = true
            });
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 1, Quantity = 3 }
            }
        };

        // Act
        var resultado = await servicio.RegistrarPedidoAsync(dto);

        // Assert
        Assert.NotNull(resultado);
        Assert.Equal(450.00m, resultado.Total);
        Assert.Single(resultado.Detalle);
        Assert.Equal(3, resultado.Detalle[0].Quantity);
        Assert.Equal(150.00m, resultado.Detalle[0].UnitPrice);
        Assert.Equal(450.00m, resultado.Detalle[0].Subtotal);

        // Verificar descuento en base de datos
        using var contextoVerificacion = CrearContexto();
        var productoActualizado = await contextoVerificacion.Products.FindAsync(1);
        Assert.NotNull(productoActualizado);
        Assert.Equal(7, productoActualizado.Stock);

        // Verificar movimiento de inventario registrado
        var movimiento = await contextoVerificacion.StockMovements
            .FirstOrDefaultAsync(m => m.OrderId == resultado.Id);
        Assert.NotNull(movimiento);
        Assert.Equal(-3, movimiento.Quantity);
        Assert.Equal("VENTA_PEDIDO", movimiento.MovementType);
    }

    [Fact]
    public async Task RegistrarPedido_ConProductoInexistente_LanzaNotFoundException()
    {
        // Arrange
        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 999, Quantity = 1 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => servicio.RegistrarPedidoAsync(dto));
    }

    [Fact]
    public async Task RegistrarPedido_ConProductoInactivo_LanzaBusinessRuleException()
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.Add(new Product
            {
                Id = 2,
                Name = "Producto Descontinuado",
                Price = 50.00m,
                Stock = 20,
                Active = false
            });
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 2, Quantity = 1 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.RegistrarPedidoAsync(dto));
        Assert.Contains("inactivo", ex.Message.ToLower());
    }

    [Fact]
    public async Task RegistrarPedido_ConStockInsuficiente_LanzaBusinessRuleException()
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.Add(new Product
            {
                Id = 3,
                Name = "Mouse Gamer",
                Price = 80.00m,
                Stock = 2,
                Active = true
            });
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 3, Quantity = 5 }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.RegistrarPedidoAsync(dto));
        Assert.Contains("insuficiente", ex.Message.ToLower());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public async Task RegistrarPedido_ConCantidadInvalidaMenorOCero_LanzaBusinessRuleException(int cantidadInvalida)
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.Add(new Product
            {
                Id = 4,
                Name = "Cable USB",
                Price = 15.00m,
                Stock = 50,
                Active = true
            });
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 4, Quantity = cantidadInvalida }
            }
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.RegistrarPedidoAsync(dto));
        Assert.Contains("mayor a cero", ex.Message.ToLower());
    }

    [Fact]
    public async Task RegistrarPedido_CalculaSubtotalesYTotalCorrectamenteEnElServidor()
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.AddRange(
                new Product { Id = 10, Name = "Item A", Price = 45.50m, Stock = 20, Active = true },
                new Product { Id = 11, Name = "Item B", Price = 12.00m, Stock = 30, Active = true }
            );
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 10, Quantity = 2 }, // 91.00
                new() { ProductId = 11, Quantity = 3 }  // 36.00
            }
        };

        // Act
        var resultado = await servicio.RegistrarPedidoAsync(dto);

        // Assert: 91.00 + 36.00 = 127.00
        Assert.Equal(127.00m, resultado.Total);
        Assert.Equal(2, resultado.Detalle.Count);
        Assert.Equal(91.00m, resultado.Detalle.First(d => d.ProductId == 10).Subtotal);
        Assert.Equal(36.00m, resultado.Detalle.First(d => d.ProductId == 11).Subtotal);
    }

    [Fact]
    public async Task RegistrarPedido_CuandoFallaOperacion_AplicaRollbackYNoModificaStock()
    {
        // Arrange
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.AddRange(
                new Product { Id = 20, Name = "P1 Con Stock", Price = 100m, Stock = 10, Active = true },
                new Product { Id = 21, Name = "P2 Sin Stock", Price = 200m, Stock = 1, Active = true }
            );
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new PedidosService(contextoPrueba);

        // P1 tiene stock suficiente (solicita 2 de 10), pero P2 superará el disponible (solicita 5 de 1)
        var dto = new CrearPedidoDto
        {
            Items = new List<ItemPedidoDto>
            {
                new() { ProductId = 20, Quantity = 2 },
                new() { ProductId = 21, Quantity = 5 }
            }
        };

        // Act & Assert
        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.RegistrarPedidoAsync(dto));

        // Verificar que P1 NO fue descontado debido al rollback transaccional
        using var contextoVerificacion = CrearContexto();
        var p1 = await contextoVerificacion.Products.FindAsync(20);
        Assert.NotNull(p1);
        Assert.Equal(10, p1.Stock);

        // Verificar que no se creó ningún pedido ni movimiento huérfano
        Assert.Empty(await contextoVerificacion.Orders.ToListAsync());
        Assert.Empty(await contextoVerificacion.StockMovements.ToListAsync());
    }

    [Fact]
    public async Task RegistrarPedido_Concurrente_PrevieneStockNegativo()
    {
        // Arrange: Producto con exactamente 5 unidades disponibles
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.Add(new Product
            {
                Id = 30,
                Name = "Stock Limitado",
                Price = 50.00m,
                Stock = 5,
                Active = true
            });
            await contextoInicial.SaveChangesAsync();
        }

        // Simular dos clientes intentando comprar 4 unidades simultáneamente
        var tarea1 = Task.Run(async () =>
        {
            try
            {
                using var ctx1 = CrearContexto();
                var s1 = new PedidosService(ctx1);
                await s1.RegistrarPedidoAsync(new CrearPedidoDto
                {
                    Items = new List<ItemPedidoDto> { new() { ProductId = 30, Quantity = 4 } }
                });
                return true;
            }
            catch
            {
                return false;
            }
        });

        var tarea2 = Task.Run(async () =>
        {
            try
            {
                using var ctx2 = CrearContexto();
                var s2 = new PedidosService(ctx2);
                await s2.RegistrarPedidoAsync(new CrearPedidoDto
                {
                    Items = new List<ItemPedidoDto> { new() { ProductId = 30, Quantity = 4 } }
                });
                return true;
            }
            catch
            {
                return false;
            }
        });

        var resultados = await Task.WhenAll(tarea1, tarea2);

        // Assert: No pueden haber tenido éxito ambas compras porque requerían 8 unidades y solo había 5
        int exitos = resultados.Count(r => r);
        Assert.True(exitos <= 1, $"Se esperaba como máximo 1 compra exitosa, pero ocurrieron {exitos}.");

        using var contextoVerificacion = CrearContexto();
        var prodFinal = await contextoVerificacion.Products.FindAsync(30);
        Assert.NotNull(prodFinal);
        Assert.True(prodFinal.Stock >= 0, $"El stock final fue negativo ({prodFinal.Stock}) violando la regla de negocio.");
    }
}
