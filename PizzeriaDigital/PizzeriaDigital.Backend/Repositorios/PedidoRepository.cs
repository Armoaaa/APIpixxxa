using System.Collections.Concurrent;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// Repositorio en memoria de pedidos. La actualización de estado usa
/// un lock por pedido para evitar condiciones de carrera si, por ejemplo,
/// llegara una consulta justo cuando el orquestador está cambiando el estado.
/// </summary>
public class PedidoRepository
{
    private readonly ConcurrentDictionary<Guid, Pedido> _pedidos = new();
    private readonly object _lock = new();

    public Pedido Crear(Guid clienteId, List<ItemPedido> items)
    {
        var pedido = new Pedido
        {
            Id = Guid.NewGuid(),
            ClienteId = clienteId,
            Items = items,
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoPedido.EsperaConfirmacion
        };

        _pedidos[pedido.Id] = pedido;
        return pedido;
    }

    public Pedido? Obtener(Guid id) => _pedidos.GetValueOrDefault(id);

    public void ActualizarEstado(Guid id, EstadoPedido nuevoEstado)
    {
        lock (_lock)
        {
            if (_pedidos.TryGetValue(id, out var pedido))
            {
                pedido.Estado = nuevoEstado;
                pedido.ConError = false;
                pedido.UltimoError = null;
            }
        }
    }

    public void MarcarError(Guid id, string mensaje)
    {
        lock (_lock)
        {
            if (_pedidos.TryGetValue(id, out var pedido))
            {
                pedido.ConError = true;
                pedido.UltimoError = mensaje;
            }
        }
    }
}
