using Microsoft.AspNetCore.Mvc;
using PizzeriaDigital.Backend.Repositorios;
using PizzeriaDigital.Shared.Models;

namespace PizzeriaDigital.Backend.Controllers;

[ApiController]
[Route("api/pizzas")]
public class PizzaController : ControllerBase
{
    private readonly PizzaRepository _pizzaRepository;

    public PizzaController(PizzaRepository pizzaRepository)
    {
        _pizzaRepository = pizzaRepository;
    }

    [HttpGet]
    public async Task<ActionResult<List<Pizza>>> ObtenerTodas()
    {
        List<Pizza> pizzas = await _pizzaRepository.ObtenerTodas();

        return Ok(pizzas);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<Pizza>> Obtener(int id)
    {
        Pizza? pizza = await _pizzaRepository.Obtener(id);

        if (pizza == null)
        {
            return NotFound();
        }

        return Ok(pizza);
    }
}