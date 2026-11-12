using Pedidos.Api.Models;

namespace Pedidos.Api.Repositories;

/// <summary>
/// Abstracción del almacenamiento. Hoy se usa una implementación en memoria;
/// se puede cambiar por DynamoDB o SQL Server sin tocar el servicio ni los controladores.
/// </summary>
public interface IPedidoRepository
{
    Task<IReadOnlyList<Pedido>> ObtenerTodosAsync(CancellationToken ct = default);
    Task<Pedido?> ObtenerPorIdAsync(Guid id, CancellationToken ct = default);
    Task AgregarAsync(Pedido pedido, CancellationToken ct = default);
    Task ActualizarAsync(Pedido pedido, CancellationToken ct = default);
}
