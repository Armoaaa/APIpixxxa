using Microsoft.AspNetCore.Mvc;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Controllers;

[ApiController]
[Route("api/clientes")]
public class ClienteController : ControllerBase
{
    private readonly ClienteRepository _clienteRepository;

    public ClienteController(ClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Crear([FromBody] Cliente cliente)
    {
        Cliente nuevoCliente = await _clienteRepository.Crear(
            cliente.Nombre,
            cliente.Telefono,
            cliente.Direccion);

        return CreatedAtAction(
            nameof(Obtener),
            new { id = nuevoCliente.Id },
            nuevoCliente);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Cliente>> Obtener(int id)
    {
        Cliente? cliente = await _clienteRepository.Obtener(id);

        if (cliente == null)
        {
            return NotFound();
        }

        return Ok(cliente);
    }
}