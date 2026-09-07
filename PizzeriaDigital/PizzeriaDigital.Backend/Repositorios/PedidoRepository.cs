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
    private readonly ConcurrentDictionary<int, Pedido> _pedidos = new();
    private readonly object _lock = new();
    private int _siguienteId;

    public Pedido Crear(int clienteId, List<ItemPedido> items)
    {
        var pedido = new Pedido
        {
            Id = Interlocked.Increment(ref _siguienteId),
            ClienteId = clienteId,
            Items = items,
            FechaCreacion = DateTime.UtcNow,
            Estado = EstadoPedido.EsperaConfirmacion
        };

        _pedidos[pedido.Id] = pedido;
        return pedido;
    }

    public Pedido? Obtener(int id) => _pedidos.GetValueOrDefault(id);

    public void ActualizarEstado(int id, EstadoPedido nuevoEstado)
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

    public void MarcarError(int id, string mensaje)
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
