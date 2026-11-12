using Microsoft.AspNetCore.Mvc;
using Pedidos.Api.Dtos;
using Pedidos.Api.Services;

namespace Pedidos.Api.Controllers;

[ApiController]
[Route("api/pedidos")]
[Produces("application/json")]
public class PedidosController : ControllerBase
{
    private readonly IPedidoService _service;

    public PedidosController(IPedidoService service) => _service = service;

    /// <summary>Crea un pedido nuevo (queda en estado Pendiente).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PedidoResponse>> Crear([FromBody] CrearPedidoRequest request, CancellationToken ct)
    {
        var pedido = await _service.CrearAsync(request, ct);
        return CreatedAtAction(nameof(Obtener), new { id = pedido.Id }, pedido);
    }

    /// <summary>
    /// Lista pedidos. Filtros: estado, cliente, desde, hasta.
    /// Orden: ordenarPor = fecha | estado | total | cliente; direccion = asc | desc.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<PedidoResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PedidoResponse>>> Listar([FromQuery] FiltroPedidos filtro, CancellationToken ct)
        => Ok(await _service.ListarAsync(filtro, ct));

    /// <summary>Obtiene un pedido por su id.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PedidoResponse>> Obtener(Guid id, CancellationToken ct)
    {
        var pedido = await _service.ObtenerAsync(id, ct);
        return pedido is null ? NotFound() : Ok(pedido);
    }

    /// <summary>Actualiza el estado del pedido a la siguiente etapa.</summary>
    [HttpPatch("{id:guid}/estado")]
    [ProducesResponseType(typeof(PedidoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PedidoResponse>> CambiarEstado(Guid id, [FromBody] ActualizarEstadoRequest request, CancellationToken ct)
    {
        var r = await _service.CambiarEstadoAsync(id, request.Estado!.Value, ct);
        return r.Resultado switch
        {
            ResultadoCambio.Ok => Ok(r.Pedido),
            ResultadoCambio.NoEncontrado => NotFound(new ProblemDetails { Title = "Pedido no encontrado", Detail = r.Mensaje, Status = 404 }),
            _ => Conflict(new ProblemDetails { Title = "Transición de estado no permitida", Detail = r.Mensaje, Status = 409 })
        };
    }
}
