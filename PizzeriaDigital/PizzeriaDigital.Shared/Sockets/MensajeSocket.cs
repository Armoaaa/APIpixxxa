namespace PizzeriaDigital.Shared.Sockets;

/// <summary>Un mensaje del protocolo interno por socket.</summary>
public record MensajeSocket(TipoMensaje Tipo, int PedidoId, string Detalle = "");
