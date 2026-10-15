namespace SistemaPedidosInventario.Api.DTOs;

public class ProductoBajoStockDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int Stock { get; set; }
    public bool Active { get; set; }
    public int Umbral { get; set; }
}
