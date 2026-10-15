using System.ComponentModel.DataAnnotations;

namespace SistemaPedidosInventario.Api.DTOs;

public class CrearProductoDto
{
    [Required(ErrorMessage = "El nombre del producto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El nombre no puede exceder los 150 caracteres.")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no puede exceder los 500 caracteres.")]
    public string? Description { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "El precio no puede ser negativo ni cero.")]
    public decimal Price { get; set; }

    [Range(0, int.MaxValue, ErrorMessage = "El stock inicial no puede ser negativo.")]
    public int Stock { get; set; }
}
