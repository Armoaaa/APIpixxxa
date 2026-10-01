using System.Net;
using System.Net.Sockets;
using PizzeriaDigital.Shared.Configuracion;
using PizzeriaDigital.Shared.Sockets;

// El puerto sale de Shared/Configuracion/Puertos.cs (no se cambia acá).
const int Puerto = Puertos.Cocina;

// Probabilidad de simular un fallo interno (para poder ver en acción
// los reintentos que hace el Backend). Se puede desactivar totalmente
// ejecutando el programa con el argumento "--sin-fallos".
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

    // Cada pedido se procesa por su cuenta (sin "await"), para poder
    // preparar varios a la vez sin bloquear la espera de nuevos pedidos.
    _ = ManejarPedidoAsync(clienteConectado, random, probabilidadDeFallo);
}

static async Task ManejarPedidoAsync(TcpClient clienteConectado, Random random, double probabilidadDeFallo)
{
    using (clienteConectado)
    using (var stream = clienteConectado.GetStream())
    {
        int pedidoId = 0;
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
            // Cerramos la conexión sin responder: el Backend lo ve como un
            // problema de conexión y vuelve a intentar.
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
