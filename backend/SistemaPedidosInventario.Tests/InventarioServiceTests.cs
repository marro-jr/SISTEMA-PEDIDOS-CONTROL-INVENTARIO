using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;
using SistemaPedidosInventario.Api.Services;
using Xunit;

namespace SistemaPedidosInventario.Tests;

public class InventarioServiceTests : IDisposable
{
    private readonly SqliteConnection _conexion;
    private readonly DbContextOptions<ApplicationDbContext> _opcionesContexto;

    public InventarioServiceTests()
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

    [Fact]
    public async Task ObtenerProductosBajoStock_FiltraProductosPorDebajoDeUmbral()
    {
        using (var contextoInicial = CrearContexto())
        {
            contextoInicial.Products.AddRange(
                new Product { Name = "Prod Critico 1", Price = 10m, Stock = 3, Active = true },
                new Product { Name = "Prod Critico 2", Price = 20m, Stock = 8, Active = true },
                new Product { Name = "Prod Seguro", Price = 30m, Stock = 25, Active = true },
                new Product { Name = "Prod Inactivo Critico", Price = 15m, Stock = 2, Active = false }
            );
            await contextoInicial.SaveChangesAsync();
        }

        using var contextoPrueba = CrearContexto();
        var servicio = new InventarioService(contextoPrueba);

        var resultados = (await servicio.ObtenerProductosBajoStockAsync(umbral: 10)).ToList();

        // Solo los 2 productos activos con stock <= 10
        Assert.Equal(2, resultados.Count);
        Assert.Contains(resultados, p => p.Name == "Prod Critico 1");
        Assert.Contains(resultados, p => p.Name == "Prod Critico 2");
        Assert.DoesNotContain(resultados, p => p.Name == "Prod Seguro");
        Assert.DoesNotContain(resultados, p => p.Name == "Prod Inactivo Critico");
    }

    [Fact]
    public async Task ObtenerProductosBajoStock_ConUmbralNegativo_LanzaBusinessRuleException()
    {
        using var contextoPrueba = CrearContexto();
        var servicio = new InventarioService(contextoPrueba);

        await Assert.ThrowsAsync<BusinessRuleException>(() => servicio.ObtenerProductosBajoStockAsync(-1));
    }
}
