using System.Collections.Concurrent;
using Pedidos.Api.Models;

namespace Pedidos.Api.Repositories;

/// <summary>Repositorio en memoria, seguro para acceso concurrente.</summary>
public class InMemoryPedidoRepository : IPedidoRepository
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();

    public Task<IReadOnlyList<Pedido>> ObtenerTodosAsync(CancellationToken ct = default)
        => Task.FromResult<IReadOnlyList<Pedido>>(_pedidos.Values.ToList());

    public Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default)
        => Task.FromResult(_pedidos.TryGetValue(id, out var p) ? p : null);

    public Task AgregarAsync(Pedido pedido, CancellationToken ct = default)
    {
        if (!_pedidos.TryAdd(pedido.Id, pedido))
            throw new InvalidOperationException($"Ya existe un pedido con id {pedido.Id}.");
        return Task.CompletedTask;
    }

    public Task ActualizarAsync(Pedido pedido, CancellationToken ct = default)
    {
        _pedidos[pedido.Id] = pedido;
        return Task.CompletedTask;
    }
}
