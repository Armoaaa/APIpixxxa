namespace PizzeriaDigital.Shared.Models;

/// <summary>
/// Una línea de pedido: una pizza puntual con su cantidad y subtotal.
/// Permite que un pedido tenga varias pizzas distintas.
/// </summary>
public class ItemPedido
{
    public int PizzaId { get; init; }
    public string NombrePizza { get; init; } = string.Empty;
    public int Cantidad { get; init; }
    public decimal Subtotal { get; init; }
}
