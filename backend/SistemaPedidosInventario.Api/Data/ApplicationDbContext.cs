using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Entities;

namespace SistemaPedidosInventario.Api.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> opciones) : base(opciones)
    {
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderDetail> OrderDetails => Set<OrderDetail>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración de Product
        modelBuilder.Entity<Product>(entidad =>
        {
            entidad.HasKey(p => p.Id);
            entidad.Property(p => p.Name).IsRequired().HasMaxLength(150);
            entidad.Property(p => p.Description).HasMaxLength(500);
            entidad.Property(p => p.Price).HasPrecision(18, 2);
            entidad.Property(p => p.Stock).IsRequired();
            entidad.Property(p => p.Active).IsRequired().HasDefaultValue(true);

            // Token de concurrencia optimista para SQL Server
            entidad.Property(p => p.RowVersion).IsRowVersion();
        });

        // Configuración de Order
        modelBuilder.Entity<Order>(entidad =>
        {
            entidad.HasKey(o => o.Id);
            entidad.Property(o => o.CreatedAt).IsRequired();
            entidad.Property(o => o.Total).HasPrecision(18, 2);
            entidad.Property(o => o.Status).IsRequired().HasMaxLength(50);
        });

        // Configuración de OrderDetail
        modelBuilder.Entity<OrderDetail>(entidad =>
        {
            entidad.HasKey(d => d.Id);
            entidad.Property(d => d.Quantity).IsRequired();
            entidad.Property(d => d.UnitPrice).HasPrecision(18, 2);
            entidad.Property(d => d.Subtotal).HasPrecision(18, 2);

            entidad.HasOne(d => d.Order)
                .WithMany(o => o.OrderDetails)
                .HasForeignKey(d => d.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            // Restringir eliminación de producto si tiene pedidos asociados
            entidad.HasOne(d => d.Product)
                .WithMany(p => p.OrderDetails)
                .HasForeignKey(d => d.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Configuración de StockMovement
        modelBuilder.Entity<StockMovement>(entidad =>
        {
            entidad.HasKey(sm => sm.Id);
            entidad.Property(sm => sm.MovementType).IsRequired().HasMaxLength(50);
            entidad.Property(sm => sm.CreatedAt).IsRequired();

            entidad.HasOne(sm => sm.Product)
                .WithMany(p => p.StockMovements)
                .HasForeignKey(sm => sm.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            entidad.HasOne(sm => sm.Order)
                .WithMany(o => o.StockMovements)
                .HasForeignKey(sm => sm.OrderId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}
