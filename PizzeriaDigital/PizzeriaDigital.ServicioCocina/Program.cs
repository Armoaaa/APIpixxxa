using System.Net;
using System.Net.Sockets;
using PizzeriaDigital.Shared.Sockets;

const int Puerto = 6000;

// Probabilidad de simular un fallo interno (para poder ver en acción
// los reintidos con backoff que implementa el Backend). Se puede
// desactivar totalmente pasando "--sin-fallos" como argumento.
double probabilidadDeFallo = args.Contains("--sin-fallos") ? 0.0 : 0.15;

var listener = new TcpListener(IPAddress.Any, Puerto);
listener.Start();

Console.WriteLine("======================================");
Console.WriteLine(" 🍕 Servicio de COCINA");
Console.WriteLine($" Escuchando en el puerto {Puerto}");
Console.WriteLine($" Probabilidad de fallo simulado: {probabilidadDeFallo:P0}");
Console.WriteLine("======================================");

var random = new Random();

while (true)
{
    TcpClient clienteConectado;
    try
    {
        clienteConectado = await listener.AcceptTcpClientAsync();
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[Cocina] Error aceptando conexión: {ex.Message}");
        continue;
    }

    // Cada pedido se procesa en su propia tarea, para poder atender
    // varias preparaciones "en simultáneo" sin bloquear el listener.
    _ = ManejarPedidoAsync(clienteConectado, random, probabilidadDeFallo);
}

static async Task ManejarPedidoAsync(TcpClient clienteConectado, Random random, double probabilidadDeFallo)
{
    using (clienteConectado)
    using (var stream = clienteConectado.GetStream())
    {
        Guid pedidoId = Guid.Empty;
        try
        {
            var mensaje = await SocketProtocolo.RecibirMensajeAsync(stream);
            pedidoId = mensaje.PedidoId;

            if (mensaje.Tipo != TipoMensaje.DelegarPreparacion)
            {
                Console.WriteLine($"[Cocina] Mensaje inesperado ({mensaje.Tipo}), se descarta la conexión.");
                return;
            }

            Console.WriteLine($"[Cocina] 📥 Pedido {pedidoId} recibido. Confirmando inicio...");
            await SocketProtocolo.EnviarMensajeAsync(stream, new MensajeSocket(TipoMensaje.AckPreparacion, pedidoId));

            // Simulación de un fallo interno (ej: se rompió el horno).
            // Cerramos la conexión sin responder -> del lado del Backend
            // esto se ve como un timeout, y dispara sus reintentos.
            if (random.NextDouble() < probabilidadDeFallo)
            {
                Console.WriteLine($"[Cocina] ⚠️  Fallo simulado preparando el pedido {pedidoId} (se corta la conexión).");
                return;
            }

            var segundos = random.Next(3, 8);
            Console.WriteLine($"[Cocina] 🔥 Preparando pedido {pedidoId} (demora simulada: {segundos}s)...");
            await Task.Delay(TimeSpan.FromSeconds(segundos));

            await SocketProtocolo.EnviarMensajeAsync(stream, new MensajeSocket(TipoMensaje.PedidoListo, pedidoId));
            Console.WriteLine($"[Cocina] ✅ Pedido {pedidoId} listo para reparto.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Cocina] ❌ Error inesperado atendiendo el pedido {pedidoId}: {ex.Message}");
        }
    }
}
