namespace Pedidos.Api.Models;

/// <summary>Etapas del ciclo de vida de un pedido, en orden.</summary>
public enum EstadoPedido
{
    Pendiente = 0,
    EnProceso = 1,
    Enviado = 2,
    Entregado = 3
}

public static class EstadoPedidoReglas
{
    /// <summary>
    /// Un pedido solo puede avanzar a la etapa siguiente
    /// (Pendiente → EnProceso → Enviado → Entregado).
    /// </summary>
    public static bool PuedeCambiarA(this EstadoPedido actual, EstadoPedido nuevo)
        => (int)nuevo == (int)actual + 1;
}
