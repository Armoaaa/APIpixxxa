using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Backend.Servicios;
using PizzeriaDigital.Shared.Configuracion;
using PizzeriaDigital.Shared.Dtos;
using PizzeriaDigital.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuración ----------

// Swagger: la página que documenta la API y permite probarla.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Pizzería Digital API",
        Version = "v1",
        Description = "API REST de la pizzería. Los servicios de Cocina y Reparto " +
                      "se coordinan por sockets TCP por detrás de estos endpoints."
    });
});

// Las clases que guardan los datos y coordinan el pedido.
// "Singleton" quiere decir: se crea una sola vez y se comparte en todo el programa.
builder.Services.AddSingleton<PizzaRepository>();
builder.Services.AddSingleton<ClienteRepository>();
builder.Services.AddSingleton<PedidoRepository>();
builder.Services.AddSingleton<OrquestadorPedidos>();

var app = builder.Build();

app.UseDefaultFiles();   // al entrar a "/" muestra index.html (el catálogo)
app.UseStaticFiles();    // sirve las páginas de la carpeta wwwroot (html, css, js)
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pizzería Digital API v1"));

// ---------- GET /api/pizzas ----------
app.MapGet("/api/pizzas", (PizzaRepository pizzas) =>
{
    return Results.Ok(pizzas.ObtenerTodas());
})
.WithName("ListarPizzas")
.WithSummary("Devuelve el menú de pizzas disponibles.");

// ---------- POST /api/clientes ----------
app.MapPost("/api/clientes", (ClienteRequest datos, ClienteRepository clientes) =>
{
    if (string.IsNullOrWhiteSpace(datos.Nombre) || string.IsNullOrWhiteSpace(datos.Direccion))
    {
        return Rechazar("DatosInvalidos", "Nombre y dirección son obligatorios.");
    }

    var cliente = clientes.Crear(datos.Nombre, datos.Telefono, datos.Direccion);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
})
.WithName("CrearCliente")
.WithSummary("Registra un cliente nuevo (sin contraseña).");

// ---------- GET /api/clientes/{id} ----------
app.MapGet("/api/clientes/{id:int}", (int id, ClienteRepository clientes) =>
{
    var cliente = clientes.Obtener(id);

    if (cliente == null)
    {
        return Results.NotFound(new ErrorResponse("ClienteNoEncontrado", $"No existe el cliente {id}.", 404));
    }

    return Results.Ok(cliente);
})
.WithName("ObtenerCliente");

// ---------- POST /api/pedidos ----------
app.MapPost("/api/pedidos", (
    CrearPedidoRequest datos,
    PizzaRepository pizzas,
    ClienteRepository clientes,
    PedidoRepository pedidos,
    OrquestadorPedidos orquestador) =>
{
    // 1) Revisamos que los datos del pedido tengan sentido.
    if (clientes.Obtener(datos.ClienteId) == null)
    {
        return Rechazar("ClienteNoEncontrado", $"No existe el cliente {datos.ClienteId}.");
    }

    if (datos.Items == null || datos.Items.Count == 0)
    {
        return Rechazar("PedidoVacio", "El pedido debe tener al menos una pizza.");
    }

    // 2) Armamos las líneas del pedido (pizza + cantidad + subtotal).
    var items = new List<ItemPedido>();
    foreach (var itemPedido in datos.Items)
    {
        var pizza = pizzas.Obtener(itemPedido.PizzaId);

        if (pizza == null)
        {
            return Rechazar("PizzaNoEncontrada", $"La pizza '{itemPedido.PizzaId}' no existe en el menú.");
        }

        if (itemPedido.Cantidad <= 0)
        {
            return Rechazar("CantidadInvalida", "La cantidad debe ser mayor a 0.");
        }

        items.Add(new ItemPedido
        {
            PizzaId = pizza.Id,
            NombrePizza = pizza.Nombre,
            Cantidad = itemPedido.Cantidad,
            Subtotal = pizza.Precio * itemPedido.Cantidad
        });
    }

    // 3) Guardamos el pedido.
    var pedido = pedidos.Crear(datos.ClienteId, items);
    Console.WriteLine($"[Backend] Pedido {pedido.Id} creado para el cliente {datos.ClienteId} (total: ${pedido.Total})");

    // 4) Mandamos el pedido a Cocina y Reparto SIN esperar a que terminen
    //    (por eso no hay "await"). Así el cliente recibe la confirmación enseguida
    //    y después consulta cómo va con GET /api/pedidos/{id}.
    _ = orquestador.ProcesarPedidoAsync(pedido.Id);

    return Results.Created($"/api/pedidos/{pedido.Id}", pedido);
})
.WithName("CrearPedido")
.WithSummary("Crea un pedido y dispara la orquestación (Cocina -> Reparto) en background.");

// ---------- GET /api/pedidos/{id} ----------
app.MapGet("/api/pedidos/{id:int}", (int id, PedidoRepository pedidos) =>
{
    var pedido = pedidos.Obtener(id);

    if (pedido == null)
    {
        return Results.NotFound(new ErrorResponse("PedidoNoEncontrado", $"No existe el pedido {id}.", 404));
    }

    return Results.Ok(pedido);
})
.WithName("ObtenerPedido")
.WithSummary("Consulta el estado actual de un pedido.");

// ---------- Arranque ----------
Console.WriteLine("======================================");
Console.WriteLine(" 🍕 Pizzería Digital - BACKEND");
Console.WriteLine($" Web:     {Puertos.UrlBackend}");
Console.WriteLine($" Swagger: {Puertos.UrlSwagger}");
Console.WriteLine("======================================");

// El puerto sale de Shared/Configuracion/Puertos.cs
app.Run(Puertos.UrlBackend);

// ---------- Función auxiliar ----------

// Devuelve un error 400 con el formato estándar de la API.
static IResult Rechazar(string error, string mensaje)
{
    return Results.BadRequest(new ErrorResponse(error, mensaje, 400));
}
