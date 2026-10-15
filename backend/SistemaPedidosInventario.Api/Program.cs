using Microsoft.EntityFrameworkCore;
using SistemaPedidosInventario.Api.Data;
using SistemaPedidosInventario.Api.Exceptions;
using SistemaPedidosInventario.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Configuración de base de datos SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(opciones =>
    opciones.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registro de dependencias de servicios
builder.Services.AddScoped<IProductosService, ProductosService>();

// Manejo global de excepciones estructurado
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Configuración de CORS para cliente Angular
builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy("AngularPolicy", politica =>
    {
        politica.WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Sistema de Pedidos y Control de Inventario API",
        Version = "v1",
        Description = "API REST interna para gestión de catálogo, pedidos y movimientos de existencias."
    });
});

var app = builder.Build();

// Inicialización de base de datos y carga de datos semilla
using (var scope = app.Services.CreateScope())
{
    var servicios = scope.ServiceProvider;
    var logger = servicios.GetRequiredService<ILogger<Program>>();
    try
    {
        var contexto = servicios.GetRequiredService<ApplicationDbContext>();
        await DbInitializer.InicializarAsync(contexto);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error al inicializar la base de datos SQL Server.");
    }
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "API v1");
    });
}

app.UseCors("AngularPolicy");

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
