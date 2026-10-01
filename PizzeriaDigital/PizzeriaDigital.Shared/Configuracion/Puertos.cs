namespace PizzeriaDigital.Shared.Configuracion;

/// <summary>
/// ÚNICO lugar donde se definen los puertos del proyecto.
/// Si querés cambiar un puerto (por ejemplo el del Swagger), cambialo SOLO acá.
/// Después volvé a ejecutar los servicios para que tomen el cambio.
/// </summary>
public static class Puertos
{
    // Backend: la API, las páginas web y el Swagger viven todos en este puerto.
    public const int Backend = 9000;

    // Servicios internos que hablan con el Backend por sockets.
    public const int Cocina = 9002;
    public const int Reparto = 9001;

    // Dirección donde corren los servicios internos (esta misma computadora).
    public const string Servidor = "127.0.0.1";

    // Direcciones completas, armadas con los puertos de arriba.
    public static string UrlBackend => $"http://localhost:{Backend}";
    public static string UrlSwagger => $"{UrlBackend}/swagger";
}
