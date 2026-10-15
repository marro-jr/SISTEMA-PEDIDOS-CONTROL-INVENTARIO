using System.ComponentModel.DataAnnotations;

namespace SistemaPedidosInventario.Api.DTOs;

public class CrearPedidoDto
{
    [Required(ErrorMessage = "El pedido debe contener al menos un producto.")]
    [MinLength(1, ErrorMessage = "El pedido debe tener como mínimo un ítem.")]
    public List<ItemPedidoDto> Items { get; set; } = new();
}
