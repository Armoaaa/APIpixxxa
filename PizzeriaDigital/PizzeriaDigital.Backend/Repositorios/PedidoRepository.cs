using MySqlConnector;
using PizzeriaDigital.Shared.Models;
using System.Data;
using System.Text.Json;

namespace PizzeriaDigital.Backend.Repositorios;

public class PedidoRepository
{
    private readonly string _connectionString;

    public PedidoRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PizzeriaDigital")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión PizzeriaDigital.");
    }

    public async Task<Pedido> Crear(
        int clienteId,
        List<ItemPedido> items)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "CrearPedido",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_ClienteId", clienteId);

        string itemsJson = JsonSerializer.Serialize(
            items.Select(item => new
            {
                item.PizzaId,
                item.Cantidad
            }));

        comando.Parameters.AddWithValue("@p_Items", itemsJson);

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            throw new Exception("No se pudo crear el pedido.");
        }

        Pedido pedido = new Pedido
        {
            Id = reader.GetInt32("Id"),
            ClienteId = reader.GetInt32("ClienteId"),
            FechaCreacion = reader.GetDateTime("FechaCreacion"),
            Estado = (EstadoPedido)reader.GetInt32("Estado"),
            ConError = reader.GetBoolean("ConError"),
            UltimoError = reader.IsDBNull("UltimoError")
                ? null
                : reader.GetString("UltimoError"),
            Items = new List<ItemPedido>()
        };

        await reader.NextResultAsync();

        while (await reader.ReadAsync())
        {
            ItemPedido item = new ItemPedido
            {
                PizzaId = reader.GetInt32("PizzaId"),
                NombrePizza = reader.GetString("NombrePizza"),
                Cantidad = reader.GetInt32("Cantidad"),
                Subtotal = reader.GetDecimal("Subtotal")
            };

            pedido.Items.Add(item);
        }

        return pedido;
    }

    public async Task<Pedido?> Obtener(int id)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "ObtenerPedido",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Id", id);

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        Pedido pedido = new Pedido
        {
            Id = reader.GetInt32("Id"),
            ClienteId = reader.GetInt32("ClienteId"),
            FechaCreacion = reader.GetDateTime("FechaCreacion"),
            Estado = (EstadoPedido)reader.GetInt32("Estado"),
            ConError = reader.GetBoolean("ConError"),
            UltimoError = reader.IsDBNull("UltimoError")
                ? null
                : reader.GetString("UltimoError"),
            Items = new List<ItemPedido>()
        };

        await reader.NextResultAsync();

        while (await reader.ReadAsync())
        {
            ItemPedido item = new ItemPedido
            {
                PizzaId = reader.GetInt32("PizzaId"),
                NombrePizza = reader.GetString("NombrePizza"),
                Cantidad = reader.GetInt32("Cantidad"),
                Subtotal = reader.GetDecimal("Subtotal")
            };

            pedido.Items.Add(item);
        }

        return pedido;
    }

    public async Task ActualizarEstado(
        int id,
        EstadoPedido nuevoEstado)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "ActualizarEstado",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Id", id);
        comando.Parameters.AddWithValue(
            "@p_NuevoEstado",
            (int)nuevoEstado);

        await comando.ExecuteNonQueryAsync();
    }

    public async Task MarcarError(
        int id,
        string mensaje)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "MarcarError",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Id", id);
        comando.Parameters.AddWithValue("@p_Mensaje", mensaje);

        await comando.ExecuteNonQueryAsync();
    }
}