using System.ComponentModel.DataAnnotations;
using Pedidos.Api.Models;

namespace Pedidos.Api.Dtos;

public record ClienteDto(
    [Required, StringLength(100, MinimumLength = 2)] string Nombre,
    [Required, EmailAddress] string Email,
    string? Telefono);

public record ItemPedidoDto(
    [Required, StringLength(100, MinimumLength = 1)] string Producto,
    [Range(1, 10_000)] int Cantidad,
    [Range(0.01, 100_000_000)] decimal PrecioUnitario);

public record CrearPedidoRequest(
    [Required] ClienteDto Cliente,
    [Required, MinLength(1, ErrorMessage = "El pedido debe tener al menos un producto.")] List<ItemPedidoDto> Items,
    [Required, StringLength(250, MinimumLength = 5)] string DireccionEnvio);

public record ActualizarEstadoRequest([Required] EstadoPedido? Estado);

/// <summary>Filtros y orden para listar pedidos (todos opcionales).</summary>
public class FiltroPedidos
{
    public EstadoPedido? Estado { get; set; }
    public string? Cliente { get; set; }
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    /// <summary>fecha | estado | total | cliente</summary>
    public string OrdenarPor { get; set; } = "fecha";

    /// <summary>asc | desc</summary>
    public string Direccion { get; set; } = "desc";
}

public record PedidoResponse(
    Guid Id,
    ClienteDto Cliente,
    List<ItemPedidoDto> Items,
    string DireccionEnvio,
    string Estado,
    decimal Total,
    DateTime FechaCreacion,
    DateTime FechaActualizacion)
{
    public static PedidoResponse Desde(Pedido p) => new(
        p.Id,
        new ClienteDto(p.Cliente.Nombre, p.Cliente.Email, p.Cliente.Telefono),
        p.Items.Select(i => new ItemPedidoDto(i.Producto, i.Cantidad, i.PrecioUnitario)).ToList(),
        p.DireccionEnvio,
        p.Estado.ToString(),
        p.Total,
        p.FechaCreacion,
        p.FechaActualizacion);
}
