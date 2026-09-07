namespace PizzeriaDigital.Shared.Models;

/// <summary>
/// Pedido de uno o más items de pizza, realizado por un cliente.
/// El estado avanza siempre de forma lineal (ver EstadoPedido).
/// </summary>
public class Pedido
{
    public int Id { get; init; }
    public int ClienteId { get; init; }
    public List<ItemPedido> Items { get; init; } = new();
    public DateTime FechaCreacion { get; init; }
    public EstadoPedido Estado { get; set; }
    public decimal Total => Items.Sum(i => i.Subtotal);

    /// <summary>
    /// Se completa si algún servicio interno (Cocina/Reparto) falló
    /// tras agotar los reintentos. El pedido NO gana un estado nuevo:
    /// queda "congelado" en el último estado alcanzado, pero visible
    /// para quien consulte que algo no salió bien.
    /// </summary>
    public bool ConError { get; set; }
    public string? UltimoError { get; set; }
}
