using Microsoft.AspNetCore.Mvc;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Backend.Servicios;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PedidoController : ControllerBase
{
    private readonly PedidoRepository _pedidoRepository;
    private readonly OrquestadorPedidos _orquestadorPedidos;

    public PedidoController(
        PedidoRepository pedidoRepository,
        OrquestadorPedidos orquestadorPedidos)
    {
        _pedidoRepository = pedidoRepository;
        _orquestadorPedidos = orquestadorPedidos;
    }

    [HttpPost]
    public async Task<ActionResult<Pedido>> Crear(
        [FromBody] Pedido pedido)
    {
        if (pedido.ClienteId <= 0)
        {
            return BadRequest(
                "El ClienteId debe ser mayor que 0.");
        }

        if (pedido.Items == null ||
            pedido.Items.Count == 0)
        {
            return BadRequest(
                "El pedido debe contener al menos una pizza.");
        }

        foreach (ItemPedido item in pedido.Items)
        {
            if (item.PizzaId <= 0)
            {
                return BadRequest(
                    "Todos los PizzaId deben ser mayores que 0.");
            }

            if (item.Cantidad <= 0)
            {
                return BadRequest(
                    "La cantidad de cada pizza debe ser mayor que 0.");
            }
        }

        Pedido nuevoPedido = await _pedidoRepository.Crear(
            pedido.ClienteId,
            pedido.Items);

        _ = _orquestadorPedidos.ProcesarPedidoAsync(
            nuevoPedido.Id);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = nuevoPedido.Id },
            nuevoPedido);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Pedido>> Obtener(int id)
    {
        Pedido? pedido =
            await _pedidoRepository.Obtener(id);

        if (pedido == null)
        {
            return NotFound(
                $"No existe el pedido con ID {id}.");
        }

        return Ok(pedido);
    }
}