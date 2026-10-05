using MySqlConnector;
using PizzeriaDigital.Shared.Models;
using System.Data;

namespace PizzeriaDigital.Backend.Repositorios;

public class ClienteRepository
{
    private readonly string _connectionString;

    public ClienteRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PizzeriaDigital")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión PizzeriaDigital.");
    }

    public async Task<Cliente> Crear(string nombre, string? telefono, string direccion)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "CrearCliente",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Nombre", nombre);
        comando.Parameters.AddWithValue(
            "@p_Telefono",
            telefono ?? string.Empty);
        comando.Parameters.AddWithValue("@p_Direccion", direccion);

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new Exception("No se pudo crear el cliente.");
        }

        return new Cliente
        {
            Id = reader.GetInt32("Id"),
            Nombre = reader.GetString("Nombre"),
            Telefono = reader.GetString("Telefono"),
            Direccion = reader.GetString("Direccion")
        };
    }

    public async Task<Cliente?> Obtener(int id)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "ObtenerCliente",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Id", id);

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        return new Cliente
        {
            Id = reader.GetInt32("Id"),
            Nombre = reader.GetString("Nombre"),
            Telefono = reader.GetString("Telefono"),
            Direccion = reader.GetString("Direccion")
        };
    }
}