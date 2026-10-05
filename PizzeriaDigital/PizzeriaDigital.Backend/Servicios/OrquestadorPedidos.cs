using System.Net.Sockets;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Shared.Configuracion;
using PizzeriaDigital.Shared.Models;
using PizzeriaDigital.Shared.Sockets;

namespace PizzeriaDigital.Backend.Servicios;

/// <summary>
/// Se encarga de llevar un pedido de punta a punta:
///   1) Le pide a la COCINA que lo prepare.
///   2) Le pide al REPARTO que lo entregue.
///   3) Lo marca como entregado.
///
/// Si un servicio no responde, reintenta hasta 3 veces esperando
/// cada vez el doble de tiempo (1s, 2s, 4s).
/// </summary>
public class OrquestadorPedidos
{
    private readonly PedidoRepository _pedidos;

    private const int MaxIntentos = 3;
    private const int SegundosParaConectar = 5;
    private const int SegundosParaTerminarTarea = 30;

    public OrquestadorPedidos(PedidoRepository pedidos)
    {
        _pedidos = pedidos;
    }

    public async Task ProcesarPedidoAsync(int pedidoId)
    {
        try
        {
            await PedirTareaAsync(
                servicio: "Cocina",
                puerto: Puertos.Cocina,
                pedidoId: pedidoId,
                mensajePedido: TipoMensaje.DelegarPreparacion,
                mensajeConfirmacion: TipoMensaje.AckPreparacion,
                mensajeResultado: TipoMensaje.PedidoListo,
                estadoMientrasTanto: EstadoPedido.EnPreparacion);

            await PedirTareaAsync(
                servicio: "Reparto",
                puerto: Puertos.Reparto,
                pedidoId: pedidoId,
                mensajePedido: TipoMensaje.DelegarEntrega,
                mensajeConfirmacion: TipoMensaje.AckEntrega,
                mensajeResultado: TipoMensaje.PedidoEntregado,
                estadoMientrasTanto: EstadoPedido.EnViaje);

            await _pedidos.ActualizarEstado(
                pedidoId,
                EstadoPedido.Entregado);

            Console.WriteLine(
                $"[Backend] ✅ Pedido {pedidoId} entregado con éxito.");
        }
        catch (Exception ex)
        {
            Console.WriteLine(
                $"[Backend] ❌ Pedido {pedidoId} no pudo completarse: {ex.Message}");

            await _pedidos.MarcarError(
                pedidoId,
                ex.Message);
        }
    }

    private async Task PedirTareaAsync(
        string servicio,
        int puerto,
        int pedidoId,
        TipoMensaje mensajePedido,
        TipoMensaje mensajeConfirmacion,
        TipoMensaje mensajeResultado,
        EstadoPedido estadoMientrasTanto)
    {
        int segundosDeEspera = 1;
        Exception? ultimoError = null;

        for (int intento = 1; intento <= MaxIntentos; intento++)
        {
            try
            {
                using TcpClient cliente = new TcpClient();

                using CancellationTokenSource limiteInicio =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(SegundosParaConectar));

                await cliente.ConnectAsync(
                    Puertos.Servidor,
                    puerto,
                    limiteInicio.Token);

                using NetworkStream stream = cliente.GetStream();

                await SocketProtocolo.EnviarMensajeAsync(
                    stream,
                    new MensajeSocket(mensajePedido, pedidoId),
                    limiteInicio.Token);

                Console.WriteLine(
                    $"[Backend] Pedido {pedidoId}: enviado a {servicio} " +
                    $"(intento {intento}/{MaxIntentos})");

                MensajeSocket confirmacion =
                    await SocketProtocolo.RecibirMensajeAsync(
                        stream,
                        limiteInicio.Token);

                if (confirmacion.Tipo != mensajeConfirmacion ||
                    confirmacion.PedidoId != pedidoId)
                {
                    throw new InvalidOperationException(
                        $"Respuesta inesperada de {servicio}: " +
                        $"{confirmacion.Tipo}");
                }

                await _pedidos.ActualizarEstado(
                    pedidoId,
                    estadoMientrasTanto);

                Console.WriteLine(
                    $"[Backend] Pedido {pedidoId}: {servicio} empezó. " +
                    $"Estado -> {estadoMientrasTanto}");

                using CancellationTokenSource limiteTarea =
                    new CancellationTokenSource(
                        TimeSpan.FromSeconds(SegundosParaTerminarTarea));

                MensajeSocket resultado =
                    await SocketProtocolo.RecibirMensajeAsync(
                        stream,
                        limiteTarea.Token);

                if (resultado.Tipo != mensajeResultado ||
                    resultado.PedidoId != pedidoId)
                {
                    throw new InvalidOperationException(
                        $"Resultado inesperado de {servicio}: " +
                        $"{resultado.Tipo}");
                }

                Console.WriteLine(
                    $"[Backend] Pedido {pedidoId}: " +
                    $"{servicio} terminó su tarea.");

                return;
            }
            catch (SocketException ex)
            {
                ultimoError = ex;
            }
            catch (IOException ex)
            {
                ultimoError = ex;
            }
            catch (OperationCanceledException ex)
            {
                ultimoError = ex;
            }

            Console.WriteLine(
                $"[Backend] ⚠️ Intento {intento}/{MaxIntentos} " +
                $"falló con {servicio} (pedido {pedidoId}): " +
                $"{ultimoError?.Message}");

            if (intento < MaxIntentos)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(segundosDeEspera));

                segundosDeEspera *= 2;
            }
        }

        throw new InvalidOperationException(
            $"No se pudo contactar al servicio de {servicio} " +
            $"tras {MaxIntentos} intentos.",
            ultimoError);
    }
}