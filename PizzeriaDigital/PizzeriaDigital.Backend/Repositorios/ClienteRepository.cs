using System.Collections.Concurrent;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>Repositorio en memoria de clientes (thread-safe).</summary>
public class ClienteRepository
{
    private readonly ConcurrentDictionary<int, Cliente> _clientes = new();
    private int _siguienteId;

    public Cliente Crear(string nombre, string? telefono, string direccion)
    {
        var cliente = new Cliente
        {
            Id = Interlocked.Increment(ref _siguienteId),
            Nombre = nombre,
            Telefono = telefono ?? string.Empty,
            Direccion = direccion
        };

        _clientes[cliente.Id] = cliente;
        return cliente;
    }

    public Cliente? Obtener(int id) => _clientes.GetValueOrDefault(id);
}
