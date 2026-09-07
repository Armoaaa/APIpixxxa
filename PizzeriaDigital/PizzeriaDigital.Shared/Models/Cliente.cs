namespace PizzeriaDigital.Shared.Models;

/// <summary>
/// Cliente de la pizzería. A propósito NO tiene contraseña ni ningún
/// dato de autenticación: la consigna pide un cliente simple, sin login.
/// </summary>
public class Cliente
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string Telefono { get; init; } = string.Empty;
    public string Direccion { get; init; } = string.Empty;
}
