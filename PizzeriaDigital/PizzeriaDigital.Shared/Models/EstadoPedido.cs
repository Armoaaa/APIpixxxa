namespace PizzeriaDigital.Shared.Models;

/// <summary>
/// Estados posibles de un pedido. La transición es siempre lineal
/// (no se puede "saltar" estados): 
/// EsperaConfirmacion -> EnPreparacion -> EnViaje -> Entregado
/// </summary>
public enum EstadoPedido
{
    EsperaConfirmacion = 0,
    EnPreparacion = 1,
    EnViaje = 2,
    Entregado = 3
}
