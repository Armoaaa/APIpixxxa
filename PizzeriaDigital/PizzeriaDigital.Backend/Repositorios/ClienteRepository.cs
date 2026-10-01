using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// Guarda los clientes en memoria (se pierden al apagar el programa).
/// El "lock" hace que solo una tarea a la vez toque los datos,
/// así no se mezclan si llegan dos pedidos al mismo tiempo.
/// </summary>
public class ClienteRepository
{
    private readonly Dictionary<int, Cliente> _clientes = new Dictionary<int, Cliente>();
    private readonly object _candado = new object();
    private int _ultimoId = 0;

    public Cliente Crear(string nombre, string? telefono, string direccion)
    {
        lock (_candado)
        {
            _ultimoId++;

            var cliente = new Cliente
            {
                Id = _ultimoId,
                Nombre = nombre,
                Telefono = telefono ?? string.Empty,
                Direccion = direccion
            };

            _clientes[cliente.Id] = cliente;
            return cliente;
        }
    }

    // Devuelve el cliente, o null si no existe.
    public Cliente? Obtener(int id)
    {
        lock (_candado)
        {
            _clientes.TryGetValue(id, out var cliente);
            return cliente;
        }
    }
}
