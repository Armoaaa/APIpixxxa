using System.Net.Sockets;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Shared.Models;
using PizzeriaDigital.Shared.Sockets;

namespace PizzeriaDigital.Backend.Servicios;

/// <summary>
/// Orquesta el ciclo de vida completo de un pedido, delegando a los
/// servicios internos (Cocina, Reparto) por socket TCP.
///
/// Aplica las buenas prácticas definidas en la Etapa 2 (Actividad 5):
///  - async/await de punta a punta (nunca .Result/.Wait())
///  - timeouts explícitos vía CancellationToken
///  - reintentos con backoff exponencial SOLO ante fallos transitorios
///    (SocketException, timeout), nunca ante errores de protocolo
///  - logging estructurado con el id del pedido y el número de intento
///  - el pedido nunca queda "colgado" en memoria: si todo falla, se
///    marca con ConError y el motivo, pero el proceso sigue vivo
/// </summary>
public class OrquestadorPedidos
{
    private readonly PedidoRepository _pedidos;
    private readonly ILogger<OrquestadorPedidos> _logger;
    private readonly IConfiguration _config;

    private const int MaxIntentos = 3;
    private static readonly TimeSpan TimeoutConexion = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan TimeoutTareaCompleta = TimeSpan.FromSeconds(30);

    public OrquestadorPedidos(PedidoRepository pedidos, ILogger<OrquestadorPedidos> logger, IConfiguration config)
    {
        _pedidos = pedidos;
        _logger = logger;
        _config = config;
    }

    public async Task ProcesarPedidoAsync(Guid pedidoId, CancellationToken ct = default)
    {
        try
        {
            var cocinaHost = _config["ServiciosInternos:CocinaHost"] ?? "127.0.0.1";
            var cocinaPuerto = _config.GetValue<int>("ServiciosInternos:CocinaPuerto", 6000);
            var repartoHost = _config["ServiciosInternos:RepartoHost"] ?? "127.0.0.1";
            var repartoPuerto = _config.GetValue<int>("ServiciosInternos:RepartoPuerto", 6001);

            await DelegarYEsperarAsync(
                pedidoId, "Cocina", cocinaHost, cocinaPuerto,
                TipoMensaje.DelegarPreparacion, TipoMensaje.AckPreparacion, TipoMensaje.PedidoListo,
                EstadoPedido.EnPreparacion, ct);

            await DelegarYEsperarAsync(
                pedidoId, "Reparto", repartoHost, repartoPuerto,
                TipoMensaje.DelegarEntrega, TipoMensaje.AckEntrega, TipoMensaje.PedidoEntregado,
                EstadoPedido.EnViaje, ct);

            _pedidos.ActualizarEstado(pedidoId, EstadoPedido.Entregado);
            _logger.LogInformation("Pedido {Id} entregado con éxito.", pedidoId);
        }
        catch (Exception ex)
        {
            // Punto único donde una falla de cualquier servicio interno
            // termina de "explotar": se loguea con contexto completo y
            // se deja constancia en el pedido, sin tumbar el backend.
            _logger.LogError(ex, "Pedido {Id} no pudo completarse.", pedidoId);
            _pedidos.MarcarError(pedidoId, ex.Message);
        }
    }

    private async Task DelegarYEsperarAsync(
        Guid pedidoId,
        string nombreServicio,
        string host,
        int puerto,
        TipoMensaje mensajeDelegar,
        TipoMensaje ackEsperado,
        TipoMensaje resultadoEsperado,
        EstadoPedido estadoIntermedio,
        CancellationToken ct)
    {
        var delay = TimeSpan.FromSeconds(1);
        Exception? ultimoError = null;

        for (int intento = 1; intento <= MaxIntentos; intento++)
        {
            try
            {
                using var cliente = new TcpClient();

                using var ctsConexion = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsConexion.CancelAfter(TimeoutConexion);
                await cliente.ConnectAsync(host, puerto, ctsConexion.Token);

                using var stream = cliente.GetStream();

                await SocketProtocolo.EnviarMensajeAsync(stream, new MensajeSocket(mensajeDelegar, pedidoId), ctsConexion.Token);
                _logger.LogInformation("Pedido {Id}: delegado a {Servicio} (intento {Intento}/{Max})",
                    pedidoId, nombreServicio, intento, MaxIntentos);

                var ack = await SocketProtocolo.RecibirMensajeAsync(stream, ctsConexion.Token);
                if (ack.Tipo != ackEsperado)
                    throw new InvalidOperationException($"Respuesta inesperada de {nombreServicio}: {ack.Tipo}");

                _pedidos.ActualizarEstado(pedidoId, estadoIntermedio);
                _logger.LogInformation("Pedido {Id}: {Servicio} confirmó inicio de tarea. Estado -> {Estado}",
                    pedidoId, nombreServicio, estadoIntermedio);

                // Timeout más largo para la tarea real (preparar/entregar toma tiempo)
                using var ctsTarea = CancellationTokenSource.CreateLinkedTokenSource(ct);
                ctsTarea.CancelAfter(TimeoutTareaCompleta);
                var resultado = await SocketProtocolo.RecibirMensajeAsync(stream, ctsTarea.Token);

                if (resultado.Tipo != resultadoEsperado)
                    throw new InvalidOperationException($"Resultado inesperado de {nombreServicio}: {resultado.Tipo}");

                _logger.LogInformation("Pedido {Id}: {Servicio} completó su tarea.", pedidoId, nombreServicio);
                return; // éxito: salimos del método sin más reintentos
            }
            catch (Exception ex) when (ex is SocketException or OperationCanceledException or IOException)
            {
                // Solo reintentamos fallos TRANSITORIOS (red, timeout, conexión
                // rechazada). Un error de protocolo (InvalidOperationException)
                // se deja propagar directamente: reintentar no lo va a arreglar.
                ultimoError = ex;
                _logger.LogWarning(
                    "Intento {Intento}/{Max} falló contactando a {Servicio} para el pedido {Id}: {Motivo}",
                    intento, MaxIntentos, nombreServicio, pedidoId, ex.Message);

                if (intento < MaxIntentos)
                {
                    await Task.Delay(delay, ct);
                    delay *= 2; // backoff exponencial: 1s, 2s, 4s...
                }
            }
        }

        throw new InvalidOperationException(
            $"No se pudo contactar al servicio de {nombreServicio} tras {MaxIntentos} intentos.", ultimoError);
    }
}
