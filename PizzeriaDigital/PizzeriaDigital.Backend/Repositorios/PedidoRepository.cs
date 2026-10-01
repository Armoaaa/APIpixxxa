using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// Guarda los pedidos en memoria (se pierden al apagar el programa).
/// El "lock" hace que solo una tarea a la vez toque los datos: así no se
/// pisan, por ejemplo, cuando alguien consulta un pedido justo mientras
/// el orquestador le está cambiando el estado.
/// </summary>
public class PedidoRepository
{
    private readonly Dictionary<int, Pedido> _pedidos = new Dictionary<int, Pedido>();
    private readonly object _candado = new object();
    private int _ultimoId = 0;

    public Pedido Crear(int clienteId, List<ItemPedido> items)
    {
        lock (_candado)
        {
            _ultimoId++;

            var pedido = new Pedido
            {
                Id = _ultimoId,
                ClienteId = clienteId,
                Items = items,
                FechaCreacion = DateTime.UtcNow,
                Estado = EstadoPedido.EsperaConfirmacion
            };

            _pedidos[pedido.Id] = pedido;
            return pedido;
        }
    }

    // Devuelve el pedido, o null si no existe.
    public Pedido? Obtener(int id)
    {
        lock (_candado)
        {
            _pedidos.TryGetValue(id, out var pedido);
            return pedido;
        }
    }

    public void ActualizarEstado(int id, EstadoPedido nuevoEstado)
    {
        lock (_candado)
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
        lock (_candado)
        {
            if (_pedidos.TryGetValue(id, out var pedido))
            {
                pedido.ConError = true;
                pedido.UltimoError = mensaje;
            }
        }
    }
}
