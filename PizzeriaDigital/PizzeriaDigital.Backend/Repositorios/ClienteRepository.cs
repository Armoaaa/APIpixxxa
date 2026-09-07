using System.Collections.Concurrent;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>Repositorio en memoria de clientes (thread-safe).</summary>
public class ClienteRepository
{
    private readonly ConcurrentDictionary<Guid, Cliente> _clientes = new();

    public Cliente Crear(string nombre, string? telefono, string direccion)
    {
        var cliente = new Cliente
        {
            Id = Guid.NewGuid(),
            Nombre = nombre,
            Telefono = telefono ?? string.Empty,
            Direccion = direccion
        };

        _clientes[cliente.Id] = cliente;
        return cliente;
    }

    public Cliente? Obtener(Guid id) => _clientes.GetValueOrDefault(id);
}
