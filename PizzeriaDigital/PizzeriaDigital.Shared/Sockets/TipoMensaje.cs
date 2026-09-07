namespace PizzeriaDigital.Shared.Sockets;

/// <summary>
/// Protocolo interno (Backend &lt;-&gt; Cocina / Backend &lt;-&gt; Reparto).
/// Cada mensaje se serializa como texto plano: "TIPO|pedidoId|detalle"
/// </summary>
public enum TipoMensaje
{
    DelegarPreparacion,
    AckPreparacion,
    PedidoListo,
    DelegarEntrega,
    AckEntrega,
    PedidoEntregado,
    Error
}
