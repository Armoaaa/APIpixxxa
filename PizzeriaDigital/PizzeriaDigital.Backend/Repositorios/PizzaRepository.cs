using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// El menú de la pizzería, guardado en memoria.
/// Cada pizza tiene un Id fijo para poder referenciarla desde un pedido.
/// </summary>
public class PizzaRepository
{
    private readonly List<Pizza> _pizzas = new List<Pizza>
    {
        new Pizza { Id = 1, Nombre = "Muzzarella", Tamano = "Grande", Precio = 8500m,
                    Ingredientes = new List<string> { "Muzzarella", "Salsa de tomate", "Orégano" } },

        new Pizza { Id = 2, Nombre = "Napolitana", Tamano = "Grande", Precio = 9800m,
                    Ingredientes = new List<string> { "Muzzarella", "Tomate", "Ajo", "Orégano" } },

        new Pizza { Id = 3, Nombre = "Fugazzeta", Tamano = "Grande", Precio = 9500m,
                    Ingredientes = new List<string> { "Muzzarella", "Cebolla" } },

        new Pizza { Id = 4, Nombre = "Especial", Tamano = "Grande", Precio = 11200m,
                    Ingredientes = new List<string> { "Muzzarella", "Jamón", "Morrones", "Aceitunas" } },

        new Pizza { Id = 5, Nombre = "Cuatro Quesos", Tamano = "Mediana", Precio = 9900m,
                    Ingredientes = new List<string> { "Muzzarella", "Provolone", "Roquefort", "Parmesano" } },
    };

    public List<Pizza> ObtenerTodas()
    {
        return _pizzas;
    }

    // Devuelve la pizza con ese Id, o null si no existe.
    public Pizza? Obtener(int id)
    {
        foreach (var pizza in _pizzas)
        {
            if (pizza.Id == id)
            {
                return pizza;
            }
        }

        return null;
    }
}
