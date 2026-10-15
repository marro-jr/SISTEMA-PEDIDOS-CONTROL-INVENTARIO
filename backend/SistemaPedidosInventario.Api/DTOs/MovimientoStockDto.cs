namespace SistemaPedidosInventario.Api.DTOs;

public class MovimientoStockDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string MovementType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? OrderId { get; set; }
}
