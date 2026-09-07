namespace PizzeriaDigital.Shared.Models;

/// <summary>
/// Representa una pizza del menú. Es un dato de referencia (catálogo),
/// no cambia de estado.
/// </summary>
public class Pizza
{
    public int Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Tamano { get; init; } = string.Empty;
    public decimal Precio { get; init; }
    public List<string> Ingredientes { get; init; } = new();
}
