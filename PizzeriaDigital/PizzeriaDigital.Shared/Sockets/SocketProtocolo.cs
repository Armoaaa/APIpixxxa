using System.Net.Sockets;
using System.Text;

namespace PizzeriaDigital.Shared.Sockets;

/// <summary>
/// Serializa/deserializa mensajes del protocolo interno sobre un
/// NetworkStream ya abierto. Formato de línea: "TIPO|pedidoId|detalle\n"
/// </summary>
public static class SocketProtocolo
{
    public static async Task EnviarMensajeAsync(NetworkStream stream, MensajeSocket mensaje, CancellationToken ct = default)
    {
        var linea = $"{mensaje.Tipo}|{mensaje.PedidoId}|{mensaje.Detalle}\n";
        var bytes = Encoding.UTF8.GetBytes(linea);
        await stream.WriteAsync(bytes, ct);
        await stream.FlushAsync(ct);
    }

    public static async Task<MensajeSocket> RecibirMensajeAsync(NetworkStream stream, CancellationToken ct = default)
    {
        var linea = await LeerLineaAsync(stream, ct);

        if (string.IsNullOrWhiteSpace(linea))
            throw new IOException("La conexión se cerró antes de recibir un mensaje completo");

        var partes = linea.Split('|', 3);
        if (partes.Length < 2
            || !Enum.TryParse<TipoMensaje>(partes[0], out var tipo)
            || !int.TryParse(partes[1], out var pedidoId))
        {
            throw new FormatException($"Mensaje con formato inválido: '{linea}'");
        }

        var detalle = partes.Length > 2 ? partes[2] : string.Empty;
        return new MensajeSocket(tipo, pedidoId, detalle);
    }

    /// <summary>
    /// Lee byte a byte hasta encontrar '\n'. Para un protocolo simple de
    /// mensajes cortos es suficiente y evita los problemas de buffering
    /// de StreamReader si más adelante se quisiera mezclar con lectura binaria.
    /// </summary>
    private static async Task<string> LeerLineaAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new List<byte>();
        var unByte = new byte[1];

        while (true)
        {
            int leidos = await stream.ReadAsync(unByte, ct);
            if (leidos == 0)
                break; // el otro lado cerró la conexión

            if (unByte[0] == (byte)'\n')
                break;

            buffer.Add(unByte[0]);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }
}
