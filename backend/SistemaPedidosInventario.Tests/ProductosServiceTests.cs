using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;
using SistemaPedidosInventario.Api.Services;
using Xunit;

namespace SistemaPedidosInventario.Tests;

public class ProductosServiceTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<ApplicationDbContext> _opcionesContexto;

    public ProductosServiceTests()
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        _opcionesContexto = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(_conexion)
            .Options;

        using var contexto = new ApplicationDbContext(_opcionesContexto);
        contexto.Database.EnsureCreated();
    }

    private ApplicationDbContext CrearContexto() => new(_opcionesContexto);

    public void Dispose()
    {
        _conexion.Dispose();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task CrearProducto_ConPrecioNegativoOCero_LanzaBusinessRuleException(decimal precioInvalido)
    {
        using var contexto = CrearContexto();
        var servicio = new ProductosService(contexto);

        var dto = new CrearProductoDto
        {
            Name = "Producto Prueba",
            Price = precioInvalido,
            Stock = 5
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.CrearAsync(dto));
        Assert.Contains("precio", ex.Message.ToLower());
    }

    [Fact]
    public async Task CrearProducto_ConStockNegativo_LanzaBusinessRuleException()
    {
        using var contexto = CrearContexto();
        var servicio = new ProductosService(contexto);

        var dto = new CrearProductoDto
        {
            Name = "Producto Prueba",
            Price = 25.00m,
            Stock = -5
        };

        var ex = await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.CrearAsync(dto));
        Assert.Contains("stock", ex.Message.ToLower());
    }

    [Fact]
    public async Task CrearProducto_Valido_GuardaProductoYRegistraMovimientoInicial()
    {
        using var contexto = CrearContexto();
        var servicio = new ProductosService(contexto);

        var dto = new CrearProductoDto
        {
            Name = "Teclado RGB",
            Description = "Mecánico",
            Price = 120.00m,
            Stock = 15
        };

        var creado = await servicio.CrearAsync(dto);

        Assert.NotNull(creado);
        Assert.True(creado.Id > 0);
        Assert.True(creado.Active);

        using var contextoVerificacion = CrearContexto();
        var movimiento = await contextoVerificacion.StockMovements
            .FirstOrDefaultAsync(m => m.ProductId == creado.Id);

        Assert.NotNull(movimiento);
        Assert.Equal(15, movimiento.Quantity);
        Assert.Equal("INICIAL", movimiento.MovementType);
    }

    [Fact]
    public async Task InactivarProducto_Existente_CambiaEstadoAFalso()
    {
        int idProducto;
        using (var contextoInicial = CrearContexto())
        {
            var p = new Product { Name = "Impresora", Price = 400m, Stock = 5, Active = true };
            contextoInicial.Products.Add(p);
            await contextoInicial.SaveChangesAsync();
            idProducto = p.Id;
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new ProductosService(contextoPrueba);

        var resultado = await servicio.InactivarAsync(idProducto);
        Assert.True(resultado);

        using var contextoVerificacion = CrearContexto();
        var pActualizado = await contextoVerificacion.Products.FindAsync(idProducto);
        Assert.NotNull(pActualizado);
        Assert.False(pActualizado.Active);
    }
}
