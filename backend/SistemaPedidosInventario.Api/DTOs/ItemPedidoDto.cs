using System.ComponentModel.DataAnnotations;

namespace SistemaPedidosInventario.Api.DTOs;

public class ItemPedidoDto
{
    [Range(1, int.MaxValue, ErrorMessage = "El identificador del producto es inválido.")]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "La cantidad solicitada debe ser mayor que cero.")]
    public int Quantity { get; set; }
}
