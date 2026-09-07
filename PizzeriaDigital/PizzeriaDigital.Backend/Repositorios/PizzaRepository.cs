using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Repositorios;

/// <summary>
/// Catálogo de pizzas en memoria. Los ids se generan una sola vez al
/// arrancar el proceso: el cliente debe consultar GET /api/pizzas para
/// conocerlos (no están hardcodeados en ningún lado más que acá).
/// </summary>
public class PizzaRepository
{
    private readonly List<Pizza> _pizzas;

    public PizzaRepository()
    {
        _pizzas = new List<Pizza>
        {
            new() { Id = Guid.NewGuid(), Nombre = "Muzzarella", Tamano = "Grande",
                    Precio = 8500m, Ingredientes = new() { "Muzzarella", "Salsa de tomate", "Orégano" } },
            new() { Id = Guid.NewGuid(), Nombre = "Napolitana", Tamano = "Grande",
                    Precio = 9800m, Ingredientes = new() { "Muzzarella", "Tomate", "Ajo", "Orégano" } },
            new() { Id = Guid.NewGuid(), Nombre = "Fugazzeta", Tamano = "Grande",
                    Precio = 9500m, Ingredientes = new() { "Muzzarella", "Cebolla" } },
            new() { Id = Guid.NewGuid(), Nombre = "Especial", Tamano = "Grande",
                    Precio = 11200m, Ingredientes = new() { "Muzzarella", "Jamón", "Morrones", "Aceitunas" } },
            new() { Id = Guid.NewGuid(), Nombre = "Cuatro Quesos", Tamano = "Mediana",
                    Precio = 9900m, Ingredientes = new() { "Muzzarella", "Provolone", "Roquefort", "Parmesano" } },
        };
    }

    public IReadOnlyList<Pizza> ObtenerTodas() => _pizzas;

    public Pizza? Obtener(Guid id) => _pizzas.FirstOrDefault(p => p.Id == id);
}
