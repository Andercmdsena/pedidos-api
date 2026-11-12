using Pedidos.Api.Dtos;
using Pedidos.Api.Models;
using Pedidos.Api.Repositories;

namespace Pedidos.Api.Services;

public interface IPedidoService
{
    Task<PedidoResponse> CrearAsync(CrearPedidoRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<PedidoResponse>> ListarAsync(FiltroPedidos filtro, CancellationToken ct = default);
    Task<PedidoResponse?> ObtenerAsync(Guid id, CancellationToken ct = default);
    Task<ResultadoCambioEstado> CambiarEstadoAsync(Guid id, EstadoPedido nuevoEstado, CancellationToken ct = default);
}

public enum ResultadoCambio { Ok, NoEncontrado, TransicionInvalida }

public record ResultadoCambioEstado(ResultadoCambio Resultado, PedidoResponse? Pedido = null, string? Mensaje = null);

public class PedidoService : IPedidoService
{
    private readonly IPedidoRepository _repo;
    private readonly ILogger<PedidoService> _logger;

    public PedidoService(IPedidoRepository repo, ILogger<PedidoService> logger)
    {
        _repo = repo;
        _logger = logger;
    }

    public async Task<PedidoResponse> CrearAsync(CrearPedidoRequest request, CancellationToken ct = default)
    {
        var pedido = new Pedido
        {
            Cliente = new Cliente
            {
                Nombre = request.Cliente.Nombre.Trim(),
                Email = request.Cliente.Email.Trim(),
                Telefono = request.Cliente.Telefono?.Trim()
            },
            Items = request.Items.Select(i => new ItemPedido
            {
                Producto = i.Producto.Trim(),
                Cantidad = i.Cantidad,
                PrecioUnitario = i.PrecioUnitario
            }).ToList(),
            DireccionEnvio = request.DireccionEnvio.Trim()
        };

        await _repo.AgregarAsync(pedido, ct);
        _logger.LogInformation("Pedido {PedidoId} creado para {Cliente} por {Total}", pedido.Id, pedido.Cliente.Email, pedido.Total);
        return PedidoResponse.Desde(pedido);
    }

    public async Task<IReadOnlyList<PedidoResponse>> ListarAsync(FiltroPedidos filtro, CancellationToken ct = default)
    {
        IEnumerable<Pedido> query = await _repo.ObtenerTodosAsync(ct);

        if (filtro.Estado is not null)
            query = query.Where(p => p.Estado == filtro.Estado);

        if (!string.IsNullOrWhiteSpace(filtro.Cliente))
            query = query.Where(p =>
                p.Cliente.Nombre.Contains(filtro.Cliente, StringComparison.OrdinalIgnoreCase) ||
                p.Cliente.Email.Contains(filtro.Cliente, StringComparison.OrdinalIgnoreCase));

        if (filtro.Desde is not null)
            query = query.Where(p => p.FechaCreacion >= filtro.Desde.Value.ToUniversalTime());

        if (filtro.Hasta is not null)
            query = query.Where(p => p.FechaCreacion <= filtro.Hasta.Value.ToUniversalTime());

        var asc = string.Equals(filtro.Direccion, "asc", StringComparison.OrdinalIgnoreCase);

        query = (filtro.OrdenarPor?.ToLowerInvariant()) switch
        {
            "estado" => asc ? query.OrderBy(p => p.Estado) : query.OrderByDescending(p => p.Estado),
            "total" => asc ? query.OrderBy(p => p.Total) : query.OrderByDescending(p => p.Total),
            "cliente" => asc ? query.OrderBy(p => p.Cliente.Nombre) : query.OrderByDescending(p => p.Cliente.Nombre),
            _ => asc ? query.OrderBy(p => p.FechaCreacion) : query.OrderByDescending(p => p.FechaCreacion)
        };

        return query.Select(PedidoResponse.Desde).ToList();
    }

    public async Task<PedidoResponse?> ObtenerAsync(Guid id, CancellationToken ct = default)
    {
        var pedido = await _repo.ObtenerPorIdAsync(id, ct);
        return pedido is null ? null : PedidoResponse.Desde(pedido);
    }

    public async Task<ResultadoCambioEstado> CambiarEstadoAsync(Guid id, EstadoPedido nuevoEstado, CancellationToken ct = default)
    {
        var pedido = await _repo.ObtenerPorIdAsync(id, ct);
        if (pedido is null)
            return new ResultadoCambioEstado(ResultadoCambio.NoEncontrado, Mensaje: $"No existe el pedido {id}.");

        if (!pedido.Estado.PuedeCambiarA(nuevoEstado))
            return new ResultadoCambioEstado(
                ResultadoCambio.TransicionInvalida,
                PedidoResponse.Desde(pedido),
                $"No se puede pasar de '{pedido.Estado}' a '{nuevoEstado}'. El orden es Pendiente → EnProceso → Enviado → Entregado.");

        var anterior = pedido.Estado;
        pedido.Estado = nuevoEstado;
        pedido.FechaActualizacion = DateTime.UtcNow;
        await _repo.ActualizarAsync(pedido, ct);

        _logger.LogInformation("Pedido {PedidoId}: {Anterior} -> {Nuevo}", id, anterior, nuevoEstado);
        return new ResultadoCambioEstado(ResultadoCambio.Ok, PedidoResponse.Desde(pedido));
    }
}
