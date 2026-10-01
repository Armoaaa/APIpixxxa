using System.Net;
using System.Net.Sockets;
using PizzeriaDigital.Shared.Configuracion;
using PizzeriaDigital.Shared.Sockets;

// El puerto sale de Shared/Configuracion/Puertos.cs (no se cambia acá).
const int Puerto = Puertos.Reparto;

// Probabilidad de simular un fallo en la entrega. Se puede desactivar
// ejecutando el programa con el argumento "--sin-fallos".
double probabilidadDeFallo = args.Contains("--sin-fallos") ? 0.0 : 0.10;

var listener = new TcpListener(IPAddress.Any, Puerto);
listener.Start();

Console.WriteLine("======================================");
Console.WriteLine(" 🛵 Servicio de REPARTO");
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
        Console.WriteLine($"[Reparto] Error aceptando conexión: {ex.Message}");
        continue;
    }

    _ = ManejarEntregaAsync(clienteConectado, random, probabilidadDeFallo);
}

static async Task ManejarEntregaAsync(TcpClient clienteConectado, Random random, double probabilidadDeFallo)
{
    using (clienteConectado)
    using (var stream = clienteConectado.GetStream())
    {
        int pedidoId = 0;
        try
        {
            var mensaje = await SocketProtocolo.RecibirMensajeAsync(stream);
            pedidoId = mensaje.PedidoId;

            if (mensaje.Tipo != TipoMensaje.DelegarEntrega)
            {
                Console.WriteLine($"[Reparto] Mensaje inesperado ({mensaje.Tipo}), se descarta la conexión.");
                return;
            }

            Console.WriteLine($"[Reparto] 📥 Pedido {pedidoId} recibido. Confirmando salida...");
            await SocketProtocolo.EnviarMensajeAsync(stream, new MensajeSocket(TipoMensaje.AckEntrega, pedidoId));

            if (random.NextDouble() < probabilidadDeFallo)
            {
                Console.WriteLine($"[Reparto] ⚠️  Fallo simulado entregando el pedido {pedidoId} (se corta la conexión).");
                return;
            }

            var segundos = random.Next(3, 6);
            Console.WriteLine($"[Reparto] 🛵 En camino con el pedido {pedidoId} (demora simulada: {segundos}s)...");
            await Task.Delay(TimeSpan.FromSeconds(segundos));

            await SocketProtocolo.EnviarMensajeAsync(stream, new MensajeSocket(TipoMensaje.PedidoEntregado, pedidoId));
            Console.WriteLine($"[Reparto] ✅ Pedido {pedidoId} entregado.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[Reparto] ❌ Error inesperado atendiendo el pedido {pedidoId}: {ex.Message}");
        }
    }
}
