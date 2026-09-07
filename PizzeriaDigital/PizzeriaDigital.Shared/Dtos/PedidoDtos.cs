namespace PizzeriaDigital.Shared.Dtos;

/// <summary>Body para POST /api/clientes</summary>
public record ClienteRequest(string Nombre, string? Telefono, string Direccion);

/// <summary>Una línea del pedido tal como la manda el cliente (solo id + cantidad)</summary>
public record ItemPedidoRequest(int PizzaId, int Cantidad);

/// <summary>Body para POST /api/pedidos</summary>
public record CrearPedidoRequest(int ClienteId, List<ItemPedidoRequest> Items);

/// <summary>
/// Forma estándar de error que devuelve la API (a diferencia de APIs
/// públicas como reqres.in, siempre viaja con un mensaje explícito).
/// </summary>
public record ErrorResponse(string Error, string Mensaje, int Codigo);
