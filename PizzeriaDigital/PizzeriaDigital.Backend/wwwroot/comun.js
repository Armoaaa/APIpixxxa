// =====================================================
// comun.js
//
// Lo usan las páginas:
// - index.html
// - pedido.html
//
// Contiene funciones compartidas.
// =====================================================


// =====================================================
// CONFIGURACIÓN
// =====================================================

const RUTA_API = '/api';

const CLAVE_CARRITO =
  'pizzeria-carrito';



// =====================================================
// BUSCAR ELEMENTO
// =====================================================

function obtener(id) {

  return document.getElementById(id);

}



// =====================================================
// FORMATEAR PRECIO
// =====================================================

function formatearPrecio(valor) {

  return new Intl.NumberFormat(
    'es-AR',
    {
      style: 'currency',
      currency: 'ARS',
      maximumFractionDigits: 0
    }
  ).format(valor);

}



// =====================================================
// MOSTRAR MENSAJE
// =====================================================

function mostrarMensaje(
  elemento,
  texto,
  salioBien
) {

  elemento.textContent =
    texto;


  elemento.classList.toggle(
    'ok',
    salioBien
  );

}



// =====================================================
// HABLAR CON LA API
// =====================================================
//
// metodo:
// GET  -> pedir información
// POST -> enviar información
//
// La opción cache: 'no-store' hace que el navegador
// vuelva a consultar la API.
//
// Esto es importante para el menú:
//
// BD cambia
//    ↓
// Recargás página
//    ↓
// GET /api/pizzas
//    ↓
// aparece la nueva pizza
// =====================================================

async function llamarApi(
  ruta,
  metodo = 'GET',
  datos = null
) {


  const opciones = {

    method: metodo,

    cache: 'no-store',

    headers: {
      'Content-Type': 'application/json'
    }

  };


  // Si estamos enviando datos.

  if (datos !== null) {

    opciones.body =
      JSON.stringify(datos);

  }


  // ================================================
  // Llamada al Backend
  // ================================================

  const respuesta =
    await fetch(
      RUTA_API + ruta,
      opciones
    );


  // ================================================
  // Intentar leer respuesta JSON
  // ================================================

  let cuerpo = null;


  try {

    cuerpo =
      await respuesta.json();

  } catch (error) {

    cuerpo = null;

  }



  // ================================================
  // Error HTTP
  // ================================================

  if (!respuesta.ok) {


    if (
      cuerpo &&
      cuerpo.mensaje
    ) {

      throw new Error(
        cuerpo.mensaje
      );

    }


    throw new Error(
      'No se pudo completar la operacion (' +
      respuesta.status +
      ').'
    );

  }


  return cuerpo;

}



// =====================================================
// CARRITO
// =====================================================

function leerCarrito() {


  try {


    const guardado =
      localStorage.getItem(
        CLAVE_CARRITO
      );


    if (guardado) {

      return JSON.parse(
        guardado
      );

    }


  } catch (error) {

    // Si hay un problema con localStorage,
    // empezamos con carrito vacío.

  }


  return [];

}



// =====================================================
// GUARDAR CARRITO
// =====================================================

function guardarCarrito(
  carrito
) {


  try {

    localStorage.setItem(
      CLAVE_CARRITO,
      JSON.stringify(carrito)
    );


  } catch (error) {

    // Si el navegador no permite guardar,
    // no hacemos nada.

  }

}



// =====================================================
// CAMBIAR CANTIDAD
// =====================================================
//
// diferencia:
// +1 = agregar
// -1 = quitar
// =====================================================

function cambiarCantidad(
  pizzaId,
  diferencia
) {


  let carrito =
    leerCarrito();


  const item =
    carrito.find(
      entrada =>
        entrada.pizzaId === pizzaId
    );


  // ================================================
  // La pizza ya está en el carrito
  // ================================================

  if (item) {

    item.cantidad +=
      diferencia;

  }


  // ================================================
  // La pizza todavía no estaba
  // ================================================

  else if (
    diferencia > 0
  ) {

    carrito.push({

      pizzaId: pizzaId,

      cantidad: diferencia

    });

  }


  // ================================================
  // Eliminar las cantidades 0 o negativas
  // ================================================

  carrito =
    carrito.filter(
      entrada =>
        entrada.cantidad > 0
    );


  guardarCarrito(
    carrito
  );


  return carrito;

}



// =====================================================
// CONTAR PIZZAS
// =====================================================

function contarPizzas(
  carrito
) {


  let total = 0;


  for (
    const item of carrito
  ) {

    total +=
      item.cantidad;

  }


  return total;

}



// =====================================================
// CONTADOR DEL NAV
// =====================================================

function actualizarContadorNav() {


  const etiqueta =
    obtener(
      'nav-cantidad'
    );


  if (!etiqueta) {
    return;
  }


  const cantidad =
    contarPizzas(
      leerCarrito()
    );


  etiqueta.textContent =
    cantidad;


  etiqueta.hidden =
    cantidad === 0;

}



// =====================================================
// ACTUALIZAR AL CARGAR
// =====================================================

actualizarContadorNav();