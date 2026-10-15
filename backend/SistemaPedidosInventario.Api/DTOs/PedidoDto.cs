namespace SistemaPedidosInventario.Api.DTOs;

public class PedidoDto
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<DetallePedidoDto> Detalle { get; set; } = new();
}
