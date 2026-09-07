using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Backend.Servicios;
using PizzeriaDigital.Shared.Dtos;
using PizzeriaDigital.Shared.Models;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddSingleton<PizzaRepository>();
builder.Services.AddSingleton<ClienteRepository>();
builder.Services.AddSingleton<PedidoRepository>();
builder.Services.AddSingleton<OrquestadorPedidos>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Pizzería Digital API v1"));

// ---------- GET /api/pizzas ----------
app.MapGet("/api/pizzas", (PizzaRepository repo) =>
    Results.Ok(repo.ObtenerTodas()))
    .WithName("ListarPizzas")
    .WithSummary("Devuelve el menú de pizzas disponibles.");

// ---------- POST /api/clientes ----------
app.MapPost("/api/clientes", (ClienteRequest req, ClienteRepository repo) =>
{
    if (string.IsNullOrWhiteSpace(req.Nombre) || string.IsNullOrWhiteSpace(req.Direccion))
    {
        return Results.BadRequest(new ErrorResponse(
            "DatosInvalidos", "Nombre y dirección son obligatorios.", 400));
    }

    var cliente = repo.Crear(req.Nombre, req.Telefono, req.Direccion);
    return Results.Created($"/api/clientes/{cliente.Id}", cliente);
})
.WithName("CrearCliente")
.WithSummary("Registra un cliente nuevo (sin contraseña).");

// ---------- GET /api/clientes/{id} ----------
app.MapGet("/api/clientes/{id:int}", (int id, ClienteRepository repo) =>
{
    var cliente = repo.Obtener(id);
    return cliente is null
        ? Results.NotFound(new ErrorResponse("ClienteNoEncontrado", $"No existe el cliente {id}.", 404))
        : Results.Ok(cliente);
})
.WithName("ObtenerCliente");

// ---------- POST /api/pedidos ----------
app.MapPost("/api/pedidos", (
    CrearPedidoRequest req,
    PizzaRepository pizzas,
    ClienteRepository clientes,
    PedidoRepository pedidos,
    OrquestadorPedidos orquestador,
    ILogger<Program> logger) =>
{
    if (clientes.Obtener(req.ClienteId) is null)
    {
        return Results.BadRequest(new ErrorResponse(
            "ClienteNoEncontrado", $"No existe el cliente {req.ClienteId}.", 400));
    }

    if (req.Items is null || req.Items.Count == 0)
    {
        return Results.BadRequest(new ErrorResponse(
            "PedidoVacio", "El pedido debe tener al menos una pizza.", 400));
    }

    var items = new List<ItemPedido>();
    foreach (var itemReq in req.Items)
    {
        var pizza = pizzas.Obtener(itemReq.PizzaId);
        if (pizza is null)
        {
            return Results.BadRequest(new ErrorResponse(
                "PizzaNoEncontrada", $"La pizza '{itemReq.PizzaId}' no existe en el menú.", 400));
        }

        if (itemReq.Cantidad <= 0)
        {
            return Results.BadRequest(new ErrorResponse(
                "CantidadInvalida", "La cantidad debe ser mayor a 0.", 400));
        }

        items.Add(new ItemPedido
        {
            PizzaId = pizza.Id,
            NombrePizza = pizza.Nombre,
            Cantidad = itemReq.Cantidad,
            Subtotal = pizza.Precio * itemReq.Cantidad
        });
    }

    var pedido = pedidos.Crear(req.ClienteId, items);
    logger.LogInformation("Pedido {Id} creado para el cliente {ClienteId} (total: {Total})",
        pedido.Id, req.ClienteId, pedido.Total);

    // Fire-and-forget deliberado: la API responde 201 de inmediato
    // (el cliente no debería esperar a que termine TODO el ciclo del
    // pedido para recibir confirmación de que fue registrado).
    // La orquestación sigue en background y el cliente consulta el
    // avance con GET /api/pedidos/{id}.
    _ = orquestador.ProcesarPedidoAsync(pedido.Id);

    return Results.Created($"/api/pedidos/{pedido.Id}", pedido);
})
.WithName("CrearPedido")
.WithSummary("Crea un pedido y dispara la orquestación (Cocina -> Reparto) en background.");

// ---------- GET /api/pedidos/{id} ----------
app.MapGet("/api/pedidos/{id:int}", (int id, PedidoRepository pedidos) =>
{
    var pedido = pedidos.Obtener(id);
    return pedido is null
        ? Results.NotFound(new ErrorResponse("PedidoNoEncontrado", $"No existe el pedido {id}.", 404))
        : Results.Ok(pedido);
})
.WithName("ObtenerPedido")
.WithSummary("Consulta el estado actual de un pedido.");

app.Run("http://localhost:5000");
