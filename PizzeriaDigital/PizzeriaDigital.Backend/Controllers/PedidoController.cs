using Microsoft.AspNetCore.Mvc;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Backend.Servicios;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Controllers;

[ApiController]
[Route("api/pedidos")]
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
    public async Task<ActionResult<Pedido>> Crear([FromBody] Pedido pedido)
    {
        Pedido nuevoPedido = await _pedidoRepository.Crear(
            pedido.ClienteId,
            pedido.Items);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = nuevoPedido.Id },
            nuevoPedido);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Pedido>> Obtener(int id)
    {
        Pedido? pedido = await _pedidoRepository.Obtener(id);

        if (pedido == null)
        {
            return NotFound();
        }

        return Ok(pedido);
    }
}