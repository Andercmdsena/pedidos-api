using Microsoft.Extensions.Logging.Abstractions;
using Pedidos.Api.Dtos;
using Pedidos.Api.Models;
using Pedidos.Api.Repositories;
using Pedidos.Api.Services;
using Xunit;

namespace Pedidos.Api.Tests;

public class PedidoServiceTests
{
    private static PedidoService CrearServicio()
        => new(new InMemoryPedidoRepository(), NullLogger<PedidoService>.Instance);

    private static CrearPedidoRequest Request(string cliente = "Ana Pérez", decimal precio = 10m, int cantidad = 2)
        => new(
            new ClienteDto(cliente, $"{cliente.Replace(" ", ".").ToLower()}@correo.com", null),
            new List<ItemPedidoDto> { new("Teclado", cantidad, precio) },
            "Calle 123 #45-67, Bogotá");

    [Fact]
    public async Task Crear_pedido_queda_pendiente_y_calcula_total()
    {
        var service = CrearServicio();

        var pedido = await service.CrearAsync(Request(precio: 25m, cantidad: 3));

        Assert.Equal(nameof(EstadoPedido.Pendiente), pedido.Estado);
        Assert.Equal(75m, pedido.Total);
        Assert.NotEqual(Guid.Empty, pedido.Id);
    }

    [Fact]
    public async Task Cambiar_estado_a_la_siguiente_etapa_funciona()
    {
        var service = CrearServicio();
        var pedido = await service.CrearAsync(Request());

        var r = await service.CambiarEstadoAsync(pedido.Id, EstadoPedido.EnProceso);

        Assert.Equal(ResultadoCambio.Ok, r.Resultado);
        Assert.Equal(nameof(EstadoPedido.EnProceso), r.Pedido!.Estado);
    }

    [Fact]
    public async Task Saltar_etapas_no_esta_permitido()
    {
        var service = CrearServicio();
        var pedido = await service.CrearAsync(Request());

        var r = await service.CambiarEstadoAsync(pedido.Id, EstadoPedido.Entregado);

        Assert.Equal(ResultadoCambio.TransicionInvalida, r.Resultado);
    }

    [Fact]
    public async Task Flujo_completo_hasta_entregado()
    {
        var service = CrearServicio();
        var pedido = await service.CrearAsync(Request());

        foreach (var estado in new[] { EstadoPedido.EnProceso, EstadoPedido.Enviado, EstadoPedido.Entregado })
            Assert.Equal(ResultadoCambio.Ok, (await service.CambiarEstadoAsync(pedido.Id, estado)).Resultado);

        var final = await service.ObtenerAsync(pedido.Id);
        Assert.Equal(nameof(EstadoPedido.Entregado), final!.Estado);
    }

    [Fact]
    public async Task Cambiar_estado_de_pedido_inexistente_devuelve_no_encontrado()
    {
        var service = CrearServicio();

        var r = await service.CambiarEstadoAsync(Guid.NewGuid(), EstadoPedido.EnProceso);

        Assert.Equal(ResultadoCambio.NoEncontrado, r.Resultado);
    }

    [Fact]
    public async Task Listar_filtra_por_estado()
    {
        var service = CrearServicio();
        var a = await service.CrearAsync(Request("Ana Pérez"));
        await service.CrearAsync(Request("Luis Gómez"));
        await service.CambiarEstadoAsync(a.Id, EstadoPedido.EnProceso);

        var enProceso = await service.ListarAsync(new FiltroPedidos { Estado = EstadoPedido.EnProceso });

        Assert.Single(enProceso);
        Assert.Equal(a.Id, enProceso[0].Id);
    }

    [Fact]
    public async Task Listar_filtra_por_cliente_sin_importar_mayusculas()
    {
        var service = CrearServicio();
        await service.CrearAsync(Request("Ana Pérez"));
        await service.CrearAsync(Request("Luis Gómez"));

        var r = await service.ListarAsync(new FiltroPedidos { Cliente = "luis" });

        Assert.Single(r);
        Assert.Equal("Luis Gómez", r[0].Cliente.Nombre);
    }

    [Fact]
    public async Task Listar_ordena_por_total_ascendente()
    {
        var service = CrearServicio();
        await service.CrearAsync(Request(precio: 50m, cantidad: 1));
        await service.CrearAsync(Request(precio: 10m, cantidad: 1));
        await service.CrearAsync(Request(precio: 30m, cantidad: 1));

        var r = await service.ListarAsync(new FiltroPedidos { OrdenarPor = "total", Direccion = "asc" });

        Assert.Equal(new[] { 10m, 30m, 50m }, r.Select(p => p.Total));
    }

    [Fact]
    public async Task Listar_por_defecto_ordena_por_fecha_descendente()
    {
        var service = CrearServicio();
        var primero = await service.CrearAsync(Request("Ana Pérez"));
        await Task.Delay(5);
        var segundo = await service.CrearAsync(Request("Luis Gómez"));

        var r = await service.ListarAsync(new FiltroPedidos());

        Assert.Equal(segundo.Id, r[0].Id);
        Assert.Equal(primero.Id, r[1].Id);
    }

    [Theory]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.EnProceso, true)]
    [InlineData(EstadoPedido.EnProceso, EstadoPedido.Enviado, true)]
    [InlineData(EstadoPedido.Enviado, EstadoPedido.Entregado, true)]
    [InlineData(EstadoPedido.Pendiente, EstadoPedido.Enviado, false)]
    [InlineData(EstadoPedido.Entregado, EstadoPedido.Pendiente, false)]
    [InlineData(EstadoPedido.Enviado, EstadoPedido.Enviado, false)]
    public void Reglas_de_transicion(EstadoPedido actual, EstadoPedido nuevo, bool esperado)
        => Assert.Equal(esperado, actual.PuedeCambiarA(nuevo));
}
