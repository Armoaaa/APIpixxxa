// =====================================================
//  pedido.js  ->  página pedido.html
//  Datos del cliente, carrito, confirmar el pedido y seguimiento.
//  Las pizzas se eligen en el catálogo (index.html).
// =====================================================

let menu = [];            // las pizzas disponibles (para mostrar nombre y precio)
let clienteId = null;     // se completa cuando el cliente guarda sus datos

// ---------- Carrito ----------

function mostrarCarrito() {
  const carrito = leerCarrito();
  let filas = '';
  let total = 0;

  for (const item of carrito) {
    const pizza = menu.find(p => p.id === item.pizzaId);
    if (!pizza) continue;   // si la pizza ya no existe en el menú, la salteamos

    total += pizza.precio * item.cantidad;

    filas += `
      <div class="cart-row">
        <strong>${pizza.nombre}</strong>
        <small>${item.cantidad} x ${formatearPrecio(pizza.precio)}</small>
        <div class="cart-actions">
          <button type="button" aria-label="Quitar una unidad" data-quitar="${pizza.id}">−</button>
          <span>${item.cantidad}</span>
          <button type="button" aria-label="Agregar una unidad" data-sumar="${pizza.id}">+</button>
        </div>
      </div>`;
  }

  if (filas === '') {
    filas = '<p class="empty-state">Todavia no elegiste pizzas.<br><a href="index.html#menu">Ir al catalogo →</a></p>';
  }

  const cantidad = contarPizzas(carrito);
  obtener('items-carrito').innerHTML = filas;
  obtener('contador-carrito').textContent = cantidad;
  obtener('total-carrito').textContent = formatearPrecio(total);

  // Para confirmar hace falta tener pizzas Y haber guardado los datos del cliente.
  obtener('boton-confirmar').disabled = cantidad === 0 || clienteId === null;

  // Botones − y + de cada fila
  document.querySelectorAll('[data-quitar]').forEach(boton => {
    boton.addEventListener('click', () => cambiarYMostrar(Number(boton.dataset.quitar), -1));
  });
  document.querySelectorAll('[data-sumar]').forEach(boton => {
    boton.addEventListener('click', () => cambiarYMostrar(Number(boton.dataset.sumar), 1));
  });
}

function cambiarYMostrar(pizzaId, diferencia) {
  cambiarCantidad(pizzaId, diferencia);
  actualizarContadorNav();
  mostrarCarrito();
}

// ---------- Paso 1: guardar los datos del cliente ----------

obtener('formulario-cliente').addEventListener('submit', async evento => {
  evento.preventDefault();   // evita que la página se recargue
  const aviso = obtener('resultado-cliente');
  mostrarMensaje(aviso, 'Guardando tus datos...', true);

  try {
    const cliente = await llamarApi('/clientes', 'POST', {
      nombre: obtener('cliente-nombre').value.trim(),
      telefono: obtener('cliente-telefono').value.trim(),
      direccion: obtener('cliente-direccion').value.trim()
    });

    clienteId = cliente.id;
    mostrarMensaje(aviso, 'Cliente #' + cliente.id + ' listo. Ya podes confirmar el pedido.', true);
    mostrarCarrito();
  } catch (error) {
    mostrarMensaje(aviso, error.message, false);
  }
});

// ---------- Paso 2: confirmar el pedido ----------

obtener('boton-confirmar').addEventListener('click', async () => {
  const aviso = obtener('resultado-pedido');
  mostrarMensaje(aviso, 'Enviando pedido a la cocina...', true);

  try {
    const pedido = await llamarApi('/pedidos', 'POST', {
      clienteId: clienteId,
      items: leerCarrito()
    });

    mostrarMensaje(aviso, 'Pedido #' + pedido.id + ' recibido. Estado: ' + nombreDelEstado(pedido.estado) + '.', true);

    // El pedido ya se envió: vaciamos el carrito y mostramos el seguimiento.
    guardarCarrito([]);
    actualizarContadorNav();
    mostrarCarrito();

    obtener('numero-pedido').value = pedido.id;
    mostrarSeguimiento(pedido);
    obtener('seguimiento').scrollIntoView({ behavior: 'smooth', block: 'start' });
  } catch (error) {
    mostrarMensaje(aviso, error.message, false);
  }
});

// ---------- Seguimiento de un pedido ----------

function nombreDelEstado(numero) {
  const nombres = ['Espera de confirmacion', 'En preparacion', 'En viaje', 'Entregado'];
  return nombres[numero] || 'Estado desconocido';
}

function mostrarSeguimiento(pedido) {
  const caja = obtener('resultado-seguimiento');
  const estado = nombreDelEstado(pedido.estado);

  let detalle = 'Pedido #' + pedido.id + ' · ' + formatearPrecio(pedido.total);
  if (pedido.conError) {
    detalle = 'Problema: ' + pedido.ultimoError;
  }

  const icono = pedido.conError ? '!' : '✓';
  caja.innerHTML = `<span class="tracking-icon">${icono}</span><span><strong>${estado}</strong>${detalle}</span>`;
  caja.classList.toggle('con-problema', pedido.conError);
}

obtener('formulario-seguimiento').addEventListener('submit', async evento => {
  evento.preventDefault();
  const caja = obtener('resultado-seguimiento');
  caja.classList.remove('con-problema');
  caja.innerHTML = '<span class="tracking-icon">...</span><span>Buscando tu pedido...</span>';

  try {
    const numero = Number(obtener('numero-pedido').value);
    const pedido = await llamarApi('/pedidos/' + numero);
    mostrarSeguimiento(pedido);
  } catch (error) {
    caja.innerHTML = '<span class="tracking-icon">!</span><span></span>';
    caja.lastElementChild.textContent = error.message;
  }
});

// ---------- Buscar un cliente por su número ----------

obtener('formulario-buscar-cliente').addEventListener('submit', async evento => {
  evento.preventDefault();
  const aviso = obtener('resultado-buscar-cliente');

  try {
    const numero = Number(obtener('numero-cliente').value);
    const cliente = await llamarApi('/clientes/' + numero);
    mostrarMensaje(aviso, cliente.nombre + ' · ' + cliente.direccion, true);
  } catch (error) {
    mostrarMensaje(aviso, error.message, false);
  }
});

// ---------- Inicio ----------

async function iniciar() {
  try {
    menu = await llamarApi('/pizzas');
  } catch (error) {
    obtener('items-carrito').innerHTML =
      '<p class="empty-state">No pudimos cargar el menu: ' + error.message + '</p>';
    return;
  }

  // Sacamos del carrito las pizzas que ya no existan en el menú.
  const carritoValido = leerCarrito().filter(item => menu.some(p => p.id === item.pizzaId));
  guardarCarrito(carritoValido);

  actualizarContadorNav();
  mostrarCarrito();
}

iniciar();
