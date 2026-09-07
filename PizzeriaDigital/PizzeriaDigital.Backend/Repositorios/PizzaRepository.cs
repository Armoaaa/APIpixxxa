using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// Catálogo de pizzas en memoria. Los ids son enteros estables para que
/// un pedido pueda referenciar el menú de forma sencilla y reproducible.
/// </summary>
public class PizzaRepository
{
    private readonly List<Pizza> _pizzas;

    public PizzaRepository()
    {
        _pizzas = new List<Pizza>
        {
            new() { Id = 1, Nombre = "Muzzarella", Tamano = "Grande",
                    Precio = 8500m, Ingredientes = new() { "Muzzarella", "Salsa de tomate", "Orégano" } },
            new() { Id = 2, Nombre = "Napolitana", Tamano = "Grande",
                    Precio = 9800m, Ingredientes = new() { "Muzzarella", "Tomate", "Ajo", "Orégano" } },
            new() { Id = 3, Nombre = "Fugazzeta", Tamano = "Grande",
                    Precio = 9500m, Ingredientes = new() { "Muzzarella", "Cebolla" } },
            new() { Id = 4, Nombre = "Especial", Tamano = "Grande",
                    Precio = 11200m, Ingredientes = new() { "Muzzarella", "Jamón", "Morrones", "Aceitunas" } },
            new() { Id = 5, Nombre = "Cuatro Quesos", Tamano = "Mediana",
                    Precio = 9900m, Ingredientes = new() { "Muzzarella", "Provolone", "Roquefort", "Parmesano" } },
        };
    }

    public IReadOnlyList<Pizza> ObtenerTodas() => _pizzas;

        public Pizza? Obtener(int id) => _pizzas.FirstOrDefault(p => p.Id == id);
}
