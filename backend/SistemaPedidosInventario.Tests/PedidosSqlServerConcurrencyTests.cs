using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.DTOs;
using SistemaPedidosInventario.Api.Entities;
using SistemaPedidosInventario.Api.Exceptions;
using SistemaPedidosInventario.Api.Services;
using Xunit;

namespace SistemaPedidosInventario.Tests;

/// <summary>
/// Pruebas de integración aisladas contra SQL Server para verificar el comportamiento
/// nativo del token de concurrencia optimista (RowVersion / timestamp de SQL Server).
/// Utiliza una base de datos exclusiva de pruebas (SistemaPedidosInventario_TestDb).
/// </summary>
public class PedidosSqlServerConcurrencyTests
{
    private static readonly string CadenaConexion =
        Environment.GetEnvironmentVariable("TEST_SQLSERVER_CONNECTION")
        ?? "Server=localhost;Database=SistemaPedidosInventario_TestDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;";

    private static bool SqlServerDisponible()
    {
        try
        {
            var builder = new SqlConnectionStringBuilder(CadenaConexion)
            {
                InitialCatalog = "master",
                ConnectTimeout = 2
            };
            using var conexion = new SqlConnection(builder.ConnectionString);
            conexion.Open();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static ApplicationDbContext CrearContextoSqlServer()
    {
        var opciones = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer(CadenaConexion)
            .Options;

        return new ApplicationDbContext(opciones);
    }

    [Fact]
    public async Task SqlServer_ConcurrenciaOptimistaConRowVersion_LanzaDbUpdateConcurrencyException()
    {
        if (!SqlServerDisponible())
        {
            // Si el servicio local de SQL Server está detenido, se omite para no romper el pipeline general
            return;
        }

        // Asegurar base de datos exclusiva limpia
        using (var setup = CrearContextoSqlServer())
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.EnsureCreatedAsync();

            var producto = new Product
            {
                Name = "Monitor Concurrencia SQL Server",
                Price = 800m,
                Stock = 5,
                Active = true
            };
            setup.Products.Add(producto);
            await setup.SaveChangesAsync();
        }

        try
        {
            // Cargar la misma entidad en dos contextos separados para obtener idéntico RowVersion inicial
            using var contexto1 = CrearContextoSqlServer();
            using var contexto2 = CrearContextoSqlServer();

            var productoContexto1 = await contexto1.Products.FirstAsync();
            var productoContexto2 = await contexto2.Products.FirstAsync();

            Assert.NotNull(productoContexto1.RowVersion);
            Assert.NotNull(productoContexto2.RowVersion);
            Assert.Equal(productoContexto1.RowVersion, productoContexto2.RowVersion);

            // Contexto 1 descuenta 4 unidades y guarda
            productoContexto1.Stock -= 4;
            await contexto1.SaveChangesAsync();

            // En este punto, SQL Server actualizó automáticamente el timestamp en la columna RowVersion
            // Contexto 2 intenta guardar con el RowVersion antiguo
            productoContexto2.Stock -= 4;

            // Assert: SQL Server y EF Core deben arrojar DbUpdateConcurrencyException
            await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contexto2.SaveChangesAsync());

            // Verificar que en la base de datos exclusiva el stock quedó en 1 (5 - 4 = 1) y nunca negativo
            using var contextoVerificacion = CrearContextoSqlServer();
            var productoFinal = await contextoVerificacion.Products.FirstAsync();
            Assert.Equal(1, productoFinal.Stock);
        }
        finally
        {
            using var limpieza = CrearContextoSqlServer();
            await limpieza.Database.EnsureDeletedAsync();
        }
    }

    [Fact]
    public async Task SqlServer_PedidosConcurrentes_SoloUnoTieneExitoYOtroEsRechazadoPorConcurrencia()
    {
        if (!SqlServerDisponible())
        {
            return;
        }

        int productoId;
        using (var setup = CrearContextoSqlServer())
        {
            await setup.Database.EnsureDeletedAsync();
            await setup.Database.EnsureCreatedAsync();

            var producto = new Product
            {
                Name = "Procesador Alta Demanda",
                Price = 1200m,
                Stock = 5,
                Active = true
            };
            setup.Products.Add(producto);
            await setup.SaveChangesAsync();
            productoId = producto.Id;
        }

        try
        {
            var semaforo = new SemaphoreSlim(0, 2);

            var tarea1 = Task.Run(async () =>
            {
                await semaforo.WaitAsync();
                try
                {
                    using var ctx1 = CrearContextoSqlServer();
                    var servicio1 = new PedidosService(ctx1);
                    await servicio1.RegistrarPedidoAsync(new CrearPedidoDto
                    {
                        Items = new List<ItemPedidoDto> { new() { ProductId = productoId, Quantity = 4 } }
                    });
                    return (Exito: true, Error: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Exito: false, Error: (Exception?)ex);
                }
            });

            var tarea2 = Task.Run(async () =>
            {
                await semaforo.WaitAsync();
                try
                {
                    using var ctx2 = CrearContextoSqlServer();
                    var servicio2 = new PedidosService(ctx2);
                    await servicio2.RegistrarPedidoAsync(new CrearPedidoDto
                    {
                        Items = new List<ItemPedidoDto> { new() { ProductId = productoId, Quantity = 4 } }
                    });
                    return (Exito: true, Error: (Exception?)null);
                }
                catch (Exception ex)
                {
                    return (Exito: false, Error: (Exception?)ex);
                }
            });

            // Disparo concurrente
            semaforo.Release(2);

            var resultados = await Task.WhenAll(tarea1, tarea2);

            int exitos = resultados.Count(r => r.Exito);
            int fracasos = resultados.Count(r => !r.Exito);

            // Assert: Exactamente 1 éxito y 1 fallo por concurrencia o stock insuficiente
            Assert.Equal(1, exitos);
            Assert.Equal(1, fracasos);

            var fallo = resultados.First(r => !r.Exito);
            Assert.NotNull(fallo.Error);
            Assert.True(fallo.Error is DbUpdateConcurrencyException || fallo.Error is BusinessRuleException);

            using var contextoVerificacion = CrearContextoSqlServer();
            var productoFinal = await contextoVerificacion.Products.FindAsync(productoId);
            Assert.NotNull(productoFinal);
            Assert.Equal(1, productoFinal.Stock);
        }
        finally
        {
            using var limpieza = CrearContextoSqlServer();
            await limpieza.Database.EnsureDeletedAsync();
        }
    }
}
