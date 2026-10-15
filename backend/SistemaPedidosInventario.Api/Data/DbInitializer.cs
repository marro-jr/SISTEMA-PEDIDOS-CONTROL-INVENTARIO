using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Entities;

namespace SistemaPedidosInventario.Api.Data;

public static class DbInitializer
{
    public static async Task InicializarAsync(ApplicationDbContext contexto)
    {
        // Aplica migraciones pendientes si existen
        if (contexto.Database.IsRelational())
        {
            await contexto.Database.MigrateAsync();
        }
        else
        {
            await contexto.Database.EnsureCreatedAsync();
        }

        // Si ya existen productos, la base ya fue inicializada
        if (await contexto.Products.AnyAsync())
        {
            return;
        }

        var productos = new List<Product>
        {
            new() { Name = "Teclado Mecánico RGB", Description = "Switch azul, retroiluminado, distribución español", Price = 180.00m, Stock = 25, Active = true },
            new() { Name = "Mouse Inalámbrico Ergonómico", Description = "Sensor óptico 4000 DPI, batería recargable", Price = 75.50m, Stock = 40, Active = true },
            new() { Name = "Monitor LED 24 Pulgadas Full HD", Description = "Panel IPS, 75Hz, puertos HDMI y DisplayPort", Price = 520.00m, Stock = 12, Active = true },
            new() { Name = "Auriculares con Cancelación de Ruido", Description = "Conectividad Bluetooth 5.2, micrófono integrado", Price = 210.00m, Stock = 18, Active = true },
            new() { Name = "Soporte de Laptop de Aluminio", Description = "Ajustable en 6 niveles, antideslizante", Price = 65.00m, Stock = 30, Active = true },
            new() { Name = "Hub USB-C Multipuerto 7 en 1", Description = "HDMI 4K, 3x USB 3.0, lector SD/MicroSD, PD 100W", Price = 110.00m, Stock = 15, Active = true },
            new() { Name = "Webcam Full HD 1080p con Micrófono", Description = "Enfoque automático, cubierta de privacidad", Price = 145.00m, Stock = 20, Active = true },
            new() { Name = "Cable HDMI 2.1 Alta Velocidad 2m", Description = "Soporta 8K 60Hz, 4K 120Hz, conectores dorados", Price = 28.00m, Stock = 50, Active = true },
            new() { Name = "Alfombrilla para Ratón XL Antideslizante", Description = "Dimensiones 900x400mm, bordes cosidos", Price = 35.00m, Stock = 35, Active = true },
            new() { Name = "Disco Sólido SSD NVMe 1TB", Description = "Lectura hasta 3500 MB/s, formato M.2", Price = 320.00m, Stock = 8, Active = true }, // Bajo stock
            new() { Name = "Memoria RAM DDR4 16GB 3200MHz", Description = "Disipador térmico de aluminio, CL16", Price = 160.00m, Stock = 6, Active = true }, // Bajo stock
            new() { Name = "Lámpara de Escritorio LED Regulable", Description = "Brillo y temperatura de color ajustables, carga USB", Price = 85.00m, Stock = 22, Active = true },
            new() { Name = "Adaptador Bluetooth 5.3 USB", Description = "Alcance hasta 20m, baja latencia", Price = 25.00m, Stock = 4, Active = true }, // Bajo stock
            new() { Name = "Funda Impermeable para Laptop 15.6", Description = "Acolchado protector interno y bolsillo frontal", Price = 48.00m, Stock = 28, Active = true },
            new() { Name = "Limpiador de Pantallas con Microfibra", Description = "Fórmula antiestática biodegradable 250ml", Price = 15.00m, Stock = 60, Active = true },
            new() { Name = "Teclado Básico de Membrana (Descontinuado)", Description = "Modelo antiguo para pruebas de producto inactivo", Price = 40.00m, Stock = 10, Active = false }
        };

        await contexto.Products.AddRangeAsync(productos);
        await contexto.SaveChangesAsync();

        // Movimientos iniciales de apertura de inventario
        var fechaApertura = DateTime.UtcNow.AddDays(-15);
        var movimientosIniciales = productos.Select(p => new StockMovement
        {
            ProductId = p.Id,
            Quantity = p.Stock,
            MovementType = "INICIAL",
            CreatedAt = fechaApertura,
            OrderId = null
        }).ToList();

        await contexto.StockMovements.AddRangeAsync(movimientosIniciales);
        await contexto.SaveChangesAsync();

        // Pedido histórico 1
        var p1 = productos[0]; // Teclado RGB
        var p2 = productos[1]; // Mouse Ergonómico
        var detallePedido1 = new List<OrderDetail>
        {
            new() { ProductId = p1.Id, Quantity = 2, UnitPrice = p1.Price, Subtotal = 2 * p1.Price },
            new() { ProductId = p2.Id, Quantity = 1, UnitPrice = p2.Price, Subtotal = 1 * p2.Price }
        };
        var pedido1 = new Order
        {
            CreatedAt = DateTime.UtcNow.AddDays(-5),
            Status = "Completado",
            Total = detallePedido1.Sum(d => d.Subtotal),
            OrderDetails = detallePedido1
        };
        contexto.Orders.Add(pedido1);
        await contexto.SaveChangesAsync();

        // Movimientos de inventario asociados al pedido 1
        contexto.StockMovements.AddRange(
            new StockMovement { ProductId = p1.Id, Quantity = -2, MovementType = "VENTA_PEDIDO", CreatedAt = pedido1.CreatedAt, OrderId = pedido1.Id },
            new StockMovement { ProductId = p2.Id, Quantity = -1, MovementType = "VENTA_PEDIDO", CreatedAt = pedido1.CreatedAt, OrderId = pedido1.Id }
        );

        // Pedido histórico 2
        var p3 = productos[2]; // Monitor
        var p4 = productos[7]; // Cable HDMI
        var detallePedido2 = new List<OrderDetail>
        {
            new() { ProductId = p3.Id, Quantity = 1, UnitPrice = p3.Price, Subtotal = 1 * p3.Price },
            new() { ProductId = p4.Id, Quantity = 2, UnitPrice = p4.Price, Subtotal = 2 * p4.Price }
        };
        var pedido2 = new Order
        {
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            Status = "Completado",
            Total = detallePedido2.Sum(d => d.Subtotal),
            OrderDetails = detallePedido2
        };
        contexto.Orders.Add(pedido2);
        await contexto.SaveChangesAsync();

        contexto.StockMovements.AddRange(
            new StockMovement { ProductId = p3.Id, Quantity = -1, MovementType = "VENTA_PEDIDO", CreatedAt = pedido2.CreatedAt, OrderId = pedido2.Id },
            new StockMovement { ProductId = p4.Id, Quantity = -2, MovementType = "VENTA_PEDIDO", CreatedAt = pedido2.CreatedAt, OrderId = pedido2.Id }
        );

        await contexto.SaveChangesAsync();
    }
}
