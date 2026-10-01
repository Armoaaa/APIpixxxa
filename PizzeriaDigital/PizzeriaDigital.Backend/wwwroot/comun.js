// =====================================================
//  comun.js  ->  lo usan LAS DOS páginas (catálogo y pedido)
//  Acá están las funciones que se repetirían en ambas.
// =====================================================

const RUTA_API = '/api';
const CLAVE_CARRITO = 'pizzeria-carrito';   // nombre con el que se guarda el carrito en el navegador

// Atajo para buscar un elemento de la página por su id.
function obtener(id) {
  return document.getElementById(id);
}

// Convierte un número en pesos. Ejemplo: 8500 -> "$ 8.500"
function formatearPrecio(valor) {
  return new Intl.NumberFormat('es-AR', {
    style: 'currency',
    currency: 'ARS',
    maximumFractionDigits: 0
  }).format(valor);
}

// Muestra un texto debajo de un formulario.
// salioBien = true  -> texto verde
// salioBien = false -> texto rojo (error)
function mostrarMensaje(elemento, texto, salioBien) {
  elemento.textContent = texto;
  elemento.classList.toggle('ok', salioBien);
}

// ---------- Hablar con la API ----------

// Hace una consulta al Backend y devuelve la respuesta.
// metodo: 'GET' (pedir datos) o 'POST' (enviar datos).
// Si algo sale mal, lanza un error con un mensaje que se puede mostrar.
async function llamarApi(ruta, metodo = 'GET', datos = null) {
  const opciones = {
    method: metodo,
    headers: { 'Content-Type': 'application/json' }
  };

  if (datos !== null) {
    opciones.body = JSON.stringify(datos);
  }

  const respuesta = await fetch(RUTA_API + ruta, opciones);

  let cuerpo = null;
  try {
    cuerpo = await respuesta.json();
  } catch (error) {
    cuerpo = null;   // la respuesta no tenía datos
  }

  if (!respuesta.ok) {
    if (cuerpo && cuerpo.mensaje) {
      throw new Error(cuerpo.mensaje);
    }
    throw new Error('No se pudo completar la operacion (' + respuesta.status + ').');
  }

  return cuerpo;
}

// ---------- Carrito ----------
// El carrito se guarda en el navegador (localStorage) para que
// no se pierda cuando pasás del catálogo a la página del pedido.
// Es una lista de este estilo: [ { pizzaId: 1, cantidad: 2 }, { pizzaId: 3, cantidad: 1 } ]

function leerCarrito() {
  try {
    const guardado = localStorage.getItem(CLAVE_CARRITO);
    if (guardado) {
      return JSON.parse(guardado);
    }
  } catch (error) {
    // si el navegador no deja leer, arrancamos con un carrito vacío
  }
  return [];
}

function guardarCarrito(carrito) {
  try {
    localStorage.setItem(CLAVE_CARRITO, JSON.stringify(carrito));
  } catch (error) {
    // si el navegador no deja guardar, no pasa nada grave
  }
}

// Suma o resta pizzas. diferencia = +1 agrega una, -1 quita una.
// Si una pizza llega a 0 unidades, se saca del carrito.
// Devuelve el carrito actualizado.
function cambiarCantidad(pizzaId, diferencia) {
  let carrito = leerCarrito();
  const item = carrito.find(entrada => entrada.pizzaId === pizzaId);

  if (item) {
    item.cantidad += diferencia;
  } else if (diferencia > 0) {
    carrito.push({ pizzaId: pizzaId, cantidad: diferencia });
  }

  carrito = carrito.filter(entrada => entrada.cantidad > 0);
  guardarCarrito(carrito);
  return carrito;
}

// Cuenta cuántas pizzas hay en total (sumando las cantidades).
function contarPizzas(carrito) {
  let total = 0;
  for (const item of carrito) {
    total += item.cantidad;
  }
  return total;
}

// Actualiza el numerito del link "Mi pedido" del menú de arriba.
function actualizarContadorNav() {
  const etiqueta = obtener('nav-cantidad');
  if (!etiqueta) return;

  const cantidad = contarPizzas(leerCarrito());
  etiqueta.textContent = cantidad;
  etiqueta.hidden = cantidad === 0;
}

actualizarContadorNav();
