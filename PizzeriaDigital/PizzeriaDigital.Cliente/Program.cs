using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using PizzeriaDigital.Shared.Dtos;
using PizzeriaDigital.Shared.Models;

const string BaseUrl = "http://localhost:5000";
var jsonOpciones = new JsonSerializerOptions(JsonSerializerDefaults.Web);
bool modoDemo = args.Contains("--demo");

using var http = new HttpClient { BaseAddress = new Uri(BaseUrl), Timeout = TimeSpan.FromSeconds(10) };

Console.WriteLine("=======================================");
Console.WriteLine(" 🍕 Bienvenido a la Pizzería Digital");
Console.WriteLine("=======================================");

// ---------- 1) Traer el menú ----------
List<Pizza>? menu;
try
{
    menu = await http.GetFromJsonAsync<List<Pizza>>("/api/pizzas", jsonOpciones);
}
catch (HttpRequestException ex)
{
    Console.WriteLine("❌ No se pudo conectar con el backend.");
    Console.WriteLine($"   Detalle: {ex.Message}");
    Console.WriteLine("   ¿Está corriendo 'dotnet run' en PizzeriaDigital.Backend?");
    return;
}
catch (TaskCanceledException)
{
    Console.WriteLine("❌ El backend no respondió a tiempo (timeout). Probá de nuevo en un momento.");
    return;
}

if (menu is null || menu.Count == 0)
{
    Console.WriteLine("❌ El menú vino vacío. Revisá el backend.");
    return;
}

Console.WriteLine("\nMenú disponible:");
for (int i = 0; i < menu.Count; i++)
{
    var p = menu[i];
    Console.WriteLine($"  [{i}] {p.Nombre} ({p.Tamano}) - ${p.Precio}");
}

// ---------- 2) Datos del cliente ----------
string nombre, telefono, direccion;
if (modoDemo)
{
    nombre = "Cliente Demo";
    telefono = "11-5555-0000";
    direccion = "Av. Siempreviva 742";
    Console.WriteLine($"\n[Modo demo] Cliente: {nombre} / {telefono} / {direccion}");
}
else
{
    Console.Write("\nTu nombre: ");
    nombre = Console.ReadLine() ?? "";
    Console.Write("Tu teléfono: ");
    telefono = Console.ReadLine() ?? "";
    Console.Write("Tu dirección de entrega: ");
    direccion = Console.ReadLine() ?? "";
}

Cliente? cliente;
try
{
    var respuesta = await http.PostAsJsonAsync("/api/clientes",
        new ClienteRequest(nombre, telefono, direccion), jsonOpciones);

    if (!respuesta.IsSuccessStatusCode)
    {
        await MostrarErrorApiAsync(respuesta, jsonOpciones);
        return;
    }

    cliente = await respuesta.Content.ReadFromJsonAsync<Cliente>(jsonOpciones);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ Error de red registrando el cliente: {ex.Message}");
    return;
}

if (cliente is null)
{
    Console.WriteLine("❌ No se pudo registrar el cliente.");
    return;
}

Console.WriteLine($"✅ Cliente registrado con id {cliente.Id}");

// ---------- 3) Armar el pedido ----------
var items = new List<ItemPedidoRequest>();

if (modoDemo)
{
    items.Add(new ItemPedidoRequest(menu[0].Id, 2));
    if (menu.Count > 1)
        items.Add(new ItemPedidoRequest(menu[1].Id, 1));

    Console.WriteLine("[Modo demo] Pedido: 2x " + menu[0].Nombre +
        (menu.Count > 1 ? $" + 1x {menu[1].Nombre}" : ""));
}
else
{
    Console.WriteLine("\nArmá tu pedido (escribí 'fin' para terminar):");
    while (true)
    {
        Console.Write("  Número de pizza (o 'fin'): ");
        var entrada = Console.ReadLine();
        if (string.Equals(entrada, "fin", StringComparison.OrdinalIgnoreCase))
            break;

        if (!int.TryParse(entrada, out var indice) || indice < 0 || indice >= menu.Count)
        {
            Console.WriteLine("  ⚠️ Número inválido, probá de nuevo.");
            continue;
        }

        Console.Write("  Cantidad: ");
        if (!int.TryParse(Console.ReadLine(), out var cantidad) || cantidad <= 0)
        {
            Console.WriteLine("  ⚠️ Cantidad inválida, probá de nuevo.");
            continue;
        }

        items.Add(new ItemPedidoRequest(menu[indice].Id, cantidad));
        Console.WriteLine($"  ✅ Agregado: {cantidad}x {menu[indice].Nombre}");
    }
}

if (items.Count == 0)
{
    Console.WriteLine("No armaste ningún pedido. ¡Hasta la próxima!");
    return;
}

// ---------- 4) Enviar el pedido ----------
Pedido? pedido;
try
{
    var respuesta = await http.PostAsJsonAsync("/api/pedidos",
        new CrearPedidoRequest(cliente.Id, items), jsonOpciones);

    if (!respuesta.IsSuccessStatusCode)
    {
        await MostrarErrorApiAsync(respuesta, jsonOpciones);
        return;
    }

    pedido = await respuesta.Content.ReadFromJsonAsync<Pedido>(jsonOpciones);
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"❌ Error de red creando el pedido: {ex.Message}");
    return;
}

if (pedido is null)
{
    Console.WriteLine("❌ No se pudo crear el pedido.");
    return;
}

Console.WriteLine($"\n✅ Pedido creado. Id: {pedido.Id} | Total: ${pedido.Total} | Estado inicial: {pedido.Estado}");

// ---------- 5) Consultar el estado hasta que se entregue ----------
await MonitorearPedidoAsync(http, pedido.Id, jsonOpciones);

// ================== Funciones auxiliares ==================

static async Task MonitorearPedidoAsync(HttpClient http, Guid pedidoId, JsonSerializerOptions jsonOpciones)
{
    Console.WriteLine("\nSiguiendo el estado del pedido (Ctrl+C para salir)...\n");

    EstadoPedido? ultimoEstado = null;
    var maximoTiempoEspera = TimeSpan.FromSeconds(90);
    var inicio = DateTime.UtcNow;

    while (DateTime.UtcNow - inicio < maximoTiempoEspera)
    {
        try
        {
            var respuesta = await http.GetAsync($"/api/pedidos/{pedidoId}");

            if (respuesta.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine("❌ El pedido ya no existe en el backend.");
                return;
            }

            respuesta.EnsureSuccessStatusCode();
            var pedido = await respuesta.Content.ReadFromJsonAsync<Pedido>(jsonOpciones);

            if (pedido is null) continue;

            if (pedido.Estado != ultimoEstado)
            {
                Console.WriteLine($"  📦 Estado: {pedido.Estado}");
                ultimoEstado = pedido.Estado;
            }

            if (pedido.ConError)
            {
                Console.WriteLine($"  ⚠️ Atención: hubo un problema procesando el pedido: {pedido.UltimoError}");
                Console.WriteLine("     El pedido quedó detenido. Revisá el servicio y consultá el pedido más tarde.");
                return;
            }

            if (pedido.Estado == EstadoPedido.Entregado)
            {
                Console.WriteLine("\n🎉 ¡Pedido entregado! Buen provecho.");
                return;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"  ⚠️ No se pudo consultar el estado (¿backend caído?): {ex.Message}");
        }

        await Task.Delay(TimeSpan.FromSeconds(2));
    }

    Console.WriteLine("\n⏱️ Se acabó el tiempo de espera de la demo. Podés seguir consultando " +
        $"GET /api/pedidos/{pedidoId} más tarde.");
}

static async Task MostrarErrorApiAsync(HttpResponseMessage respuesta, JsonSerializerOptions jsonOpciones)
{
    try
    {
        var error = await respuesta.Content.ReadFromJsonAsync<ErrorResponse>(jsonOpciones);
        Console.WriteLine(error is not null
            ? $"❌ [{respuesta.StatusCode}] {error.Error}: {error.Mensaje}"
            : $"❌ [{respuesta.StatusCode}] Error sin detalle.");
    }
    catch
    {
        Console.WriteLine($"❌ [{respuesta.StatusCode}] Error sin poder leer el detalle.");
    }
}
