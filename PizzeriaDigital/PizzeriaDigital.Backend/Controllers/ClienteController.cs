using Microsoft.AspNetCore.Mvc;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClienteController : ControllerBase
{
    private readonly ClienteRepository _clienteRepository;

    public ClienteController(ClienteRepository clienteRepository)
    {
        _clienteRepository = clienteRepository;
    }

    [HttpPost]
    public async Task<ActionResult<Cliente>> Crear(
        [FromBody] Cliente cliente)
    {
        if (string.IsNullOrWhiteSpace(cliente.Nombre))
        {
            return BadRequest("El nombre es obligatorio.");
        }

        if (string.IsNullOrWhiteSpace(cliente.Direccion))
        {
            return BadRequest("La dirección es obligatoria.");
        }

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
        Cliente? cliente =
            await _clienteRepository.Obtener(id);

        if (cliente == null)
        {
            return NotFound(
                $"No existe el cliente con ID {id}.");
        }

        return Ok(cliente);
    }
}