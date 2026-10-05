using MySqlConnector;
using PizzeriaDigital.Shared.Models;
using System.Data;

namespace PizzeriaDigital.Backend.Repositorios;

public class PizzaRepository
{
    private readonly string _connectionString;

    public PizzaRepository(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("PizzeriaDigital")
            ?? throw new InvalidOperationException(
                "No se encontró la cadena de conexión PizzeriaDigital.");
    }

    public async Task<List<Pizza>> ObtenerTodas()
    {
        List<Pizza> pizzas = new List<Pizza>();

        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "ObtenerTodas",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            List<string> ingredientes = new List<string>();

            string ingredientesTexto = reader.GetString("Ingredientes");

            if (!string.IsNullOrWhiteSpace(ingredientesTexto))
            {
                string[] ingredientesSeparados =
                    ingredientesTexto.Split(", ");

                ingredientes.AddRange(ingredientesSeparados);
            }

            Pizza pizza = new Pizza
            {
                Id = reader.GetInt32("Id"),
                Nombre = reader.GetString("Nombre"),
                Tamano = reader.GetString("Tamano"),
                Precio = reader.GetDecimal("Precio"),
                Ingredientes = ingredientes
            };

            pizzas.Add(pizza);
        }

        return pizzas;
    }

    public async Task<Pizza?> Obtener(int id)
    {
        await using MySqlConnection conexion = new MySqlConnection(_connectionString);
        await conexion.OpenAsync();

        await using MySqlCommand comando = new MySqlCommand(
            "ObtenerPizza",
            conexion);

        comando.CommandType = CommandType.StoredProcedure;

        comando.Parameters.AddWithValue("@p_Id", id);

        await using MySqlDataReader reader = await comando.ExecuteReaderAsync();

        if (!await reader.ReadAsync())
        {
            return null;
        }

        List<string> ingredientes = new List<string>();

        string ingredientesTexto = reader.GetString("Ingredientes");

        if (!string.IsNullOrWhiteSpace(ingredientesTexto))
        {
            string[] ingredientesSeparados =
                ingredientesTexto.Split(", ");

            ingredientes.AddRange(ingredientesSeparados);
        }

        return new Pizza
        {
            Id = reader.GetInt32("Id"),
            Nombre = reader.GetString("Nombre"),
            Tamano = reader.GetString("Tamano"),
            Precio = reader.GetDecimal("Precio"),
            Ingredientes = ingredientes
        };
    }
}