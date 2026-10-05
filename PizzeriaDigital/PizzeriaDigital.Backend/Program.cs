using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Backend.Servicios;
using PizzeriaDigital.Shared.Configuracion;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(opciones =>
{
    opciones.SwaggerDoc(
        "v1",
        new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Title = "Pizzería Digital API",
            Version = "v1",
            Description = "API REST de la pizzería."
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

app.UseSwaggerUI(c =>
    c.SwaggerEndpoint(
        "/swagger/v1/swagger.json",
        "Pizzería Digital API v1"));

app.MapControllers();

Console.WriteLine("======================================");
Console.WriteLine(" 🍕 Pizzería Digital - BACKEND");
Console.WriteLine($" Web:     {Puertos.UrlBackend}");
Console.WriteLine($" Swagger: {Puertos.UrlSwagger}");
Console.WriteLine("======================================");

app.Run(Puertos.UrlBackend);