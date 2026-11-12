namespace Pedidos.Api.Models;

public class Pedido
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public Cliente Cliente { get; init; } = new();
    public List<ItemPedido> Items { get; init; } = new();
    public string DireccionEnvio { get; init; } = string.Empty;
    public EstadoPedido Estado { get; set; } = EstadoPedido.Pendiente;
    public DateTime FechaCreacion { get; init; } = DateTime.UtcNow;
    public DateTime FechaActualizacion { get; set; } = DateTime.UtcNow;

    public decimal Total => Items.Sum(i => i.Subtotal);
}

public class Cliente
{
    public string Nombre { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? Telefono { get; init; }
}

public class ItemPedido
{
    public string Producto { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal PrecioUnitario { get; init; }

    public decimal Subtotal => Cantidad * PrecioUnitario;
}
