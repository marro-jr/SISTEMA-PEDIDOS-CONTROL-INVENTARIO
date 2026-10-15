namespace SistemaPedidosInventario.Api.Exceptions;

public class NotFoundException : Exception
{
    public NotFoundException(string mensaje) : base(mensaje)
    {
    }
}
