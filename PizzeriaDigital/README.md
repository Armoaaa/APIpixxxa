# Pizzería Digital Distribuida — Etapa 3 (implementación)

Solución .NET 8 con **5 proyectos**:

| Proyecto | Tipo | Rol |
|---|---|---|
| `PizzeriaDigital.Shared` | Librería de clases | Modelos (`Pizza`, `Cliente`, `Pedido`, `EstadoPedido`), DTOs y el protocolo de mensajes por socket |
| `PizzeriaDigital.Backend` | ASP.NET Core (Minimal API) | Expone la API REST, valida y persiste en memoria, orquesta a Cocina/Reparto por socket |
| `PizzeriaDigital.ServicioCocina` | Consola | Servidor TCP que simula la cocina |
| `PizzeriaDigital.ServicioReparto` | Consola | Servidor TCP que simula el reparto |
| `PizzeriaDigital.Cliente` | Consola | Cliente que consume la API REST |

Corresponde a los diagramas y al contrato de endpoints definidos en la Etapa 1 y la Etapa 2 (ver esos documentos para el diseño completo).

## Requisitos

- [.NET SDK 8.0](https://dotnet.microsoft.com/download) o superior instalado.
- Conexión a internet la **primera vez** que se restauren paquetes (Swashbuckle para Swagger y System.Net.Http.Json), ya que no vienen con el SDK.

## Cómo correrlo (4 terminales, en este orden)

```bash
# 1) Restaurar toda la solución una sola vez
dotnet restore

# 2) Terminal 1: Cocina
dotnet run --project PizzeriaDigital.ServicioCocina

# 3) Terminal 2: Reparto
dotnet run --project PizzeriaDigital.ServicioReparto

# 4) Terminal 3: Backend (API REST + Swagger)
dotnet run --project PizzeriaDigital.Backend

# 5) Terminal 4: Cliente (consola interactiva)
dotnet run --project PizzeriaDigital.Cliente
```

> El orden importa poco salvo por el Backend: si arranca antes que Cocina/Reparto no pasa nada malo,
> simplemente el primer pedido va a agotar reintentos contra el servicio que todavía no levantó
> (¡es justamente el escenario de fallo que estudiamos en la Etapa 1 - Actividad 3!). Si Cocina/Reparto
> arrancan después, los pedidos siguientes ya se procesan bien.

Con el Backend corriendo, abrí **http://localhost:5000/swagger** para explorar y probar los
endpoints desde el navegador (Actividad 4 de la Etapa 2), o importalos en Postman.

## Modo demo (sin tipear nada)

```bash
dotnet run --project PizzeriaDigital.Cliente -- --demo
```

Registra un cliente de prueba y arma un pedido automáticamente — útil para probar rápido o para
la presentación (Actividad 5).

## Cómo probar los distintos escenarios (Actividad 5)

**1. Flujo feliz:** con los 4 procesos corriendo, hacé un pedido desde el Cliente y mirá cómo
va cambiando de estado en la consola del Cliente, mientras las consolas de Cocina y Reparto
muestran los mensajes que van recibiendo por socket.

**2. Fallos aleatorios (por defecto activados):** Cocina falla ~15% de las veces y Reparto ~10%,
simulando una caída puntual. Vas a ver en la consola del **Backend** los logs de
`Intento 1/3 falló... reintentando` con el backoff exponencial (1s, 2s, 4s) hasta que el
reintento tiene éxito o se agotan los 3 intentos.

**3. Caída total de un servicio:** cerrá la consola de Cocina (Ctrl+C) **antes** de hacer un
pedido nuevo, y hacé el pedido. Vas a ver los 3 reintentos agotarse y el pedido va a quedar
marcado con `ConError = true` y el motivo del error, sin tumbar el Backend ni al Cliente
(quien recibe un mensaje amigable en vez de un stacktrace).

**4. Sin fallos simulados (para una demo prolija):**
```bash
dotnet run --project PizzeriaDigital.ServicioCocina -- --sin-fallos
dotnet run --project PizzeriaDigital.ServicioReparto -- --sin-fallos
```

**5. Datos inválidos:** desde Swagger, probá `POST /api/pedidos` con un `pizzaId` que no exista,
o `POST /api/clientes` sin `direccion` — vas a ver el `400 Bad Request` con el cuerpo de error
explícito (`error`, `mensaje`, `codigo`) que definimos en la Etapa 2.

## Guion de demo controlada y resultados

Para una presentación reproducible, levantá Cocina y Reparto con `--sin-fallos`, luego el
Backend y finalmente ejecutá el Cliente con `--demo`.

### Escenario exitoso

El Cliente registra un cliente, crea un pedido de dos Muzzarellas y una Napolitana por `$26800`,
y observa esta secuencia:

```text
EsperaConfirmacion -> EnPreparacion -> EnViaje -> Entregado
ConError: false
```

En paralelo, el Backend registra las delegaciones y confirmaciones; Cocina informa que prepara
el pedido y Reparto informa que lo entrega. La API responde `201 Created` al registrar el pedido,
sin bloquearse hasta que termine toda la preparación.

### Escenario con fallo

Con el Backend y Reparto activos, detené Cocina antes de ejecutar otro `--demo`. El pedido queda
en `EsperaConfirmacion`, el Backend registra tres intentos con backoff y conserva el proceso vivo:

```text
Intento 1/3 falló contactando a Cocina
Intento 2/3 falló contactando a Cocina
Intento 3/3 falló contactando a Cocina
ConError: true
UltimoError: No se pudo contactar al servicio de Cocina tras 3 intentos.
```

El Cliente muestra el motivo una sola vez y termina el monitoreo, en lugar de repetir el aviso
durante el timeout completo de la demo. El pedido sigue disponible con `GET /api/pedidos/{id}`.

### Puntos para explicar

- **Responsabilidades:** REST valida y persiste; el Backend orquesta; Cocina y Reparto solo
  procesan mensajes TCP; el Cliente consulta el estado.
- **Resiliencia:** solo se reintentan errores transitorios de red o timeout, con backoff
  exponencial. Los errores de protocolo no se reintentan.
- **Consistencia:** el pedido mantiene sus cuatro estados lineales. Un fallo no inventa un
  estado nuevo: se registra con `ConError` y `UltimoError` sin tumbar la API.
- **Observabilidad:** cada log incluye pedido, servicio e intento; el protocolo de sockets es
  texto plano (`TIPO|pedidoId|detalle`) para inspección sencilla.

## Puertos usados

| Servicio | Puerto | Protocolo |
|---|---|---|
| Backend (API REST + Swagger) | 5000 | HTTP |
| Servicio Cocina | 6000 | TCP (socket) |
| Servicio Reparto | 6001 | TCP (socket) |

## Notas de diseño (por qué está hecho así)

- **`EstadoPedido` tiene exactamente 4 valores** (`EsperaConfirmacion`, `EnPreparacion`, `EnViaje`,
  `Entregado`) y solo avanza de forma lineal; nunca se "salta" un estado.
- **El Backend es el único orquestador**: Cocina y Reparto nunca se hablan entre sí ni con el
  Cliente directamente (ver Etapa 2, tabla de responsabilidades).
- **Reintentos con backoff exponencial** solo ante fallos transitorios de red
  (`SocketException`, timeout); un error de protocolo o de datos nunca se reintenta.
- **`ConError` en vez de un 5º estado**: si todo falla, el pedido no "avanza mágicamente" ni
  desaparece; queda en su último estado válido con un flag y el motivo, disponible para quien
  consulte `GET /api/pedidos/{id}`.
- El protocolo de sockets es texto plano línea por línea (`TIPO|pedidoId|detalle\n`) a propósito,
  para que se pueda ver crudo con herramientas como `nc`/Wireshark si se quiere inspeccionar.
