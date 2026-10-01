// =====================================================
//  catalogo.js  ->  página index.html
//  Solo muestra las pizzas y permite agregarlas al carrito.
//  El resto del pedido se hace en pedido.html
// =====================================================

let menu = [];

// ---------- Cargar y mostrar el menú ----------

async function cargarMenu() {
  try {
    menu = await llamarApi('/pizzas');
    mostrarMenu();
  } catch (error) {
    obtener('grilla-menu').innerHTML =
      '<p class="loading">No pudimos cargar el menu: ' + error.message + '</p>';
  }
}

function mostrarMenu() {
  let tarjetas = '';

  menu.forEach((pizza, posicion) => {
    tarjetas += `
      <article class="pizza-card">
        <span class="pizza-number">0${posicion + 1} / ${pizza.tamano}</span>
        <div class="pizza-badge">${pizza.ingredientes[0] || 'Pizza'}</div>
        <h3>${pizza.nombre}</h3>
        <p>${pizza.ingredientes.join(' · ')}</p>
        <div class="pizza-bottom">
          <span class="price">${formatearPrecio(pizza.precio)}</span>
          <button class="add-button" type="button" aria-label="Agregar ${pizza.nombre}" data-agregar="${pizza.id}">+</button>
        </div>
      </article>`;
  });

  obtener('grilla-menu').innerHTML = tarjetas;

  // A cada botón "+" le decimos qué hacer cuando lo tocan.
  const botones = document.querySelectorAll('[data-agregar]');
  botones.forEach(boton => {
    boton.addEventListener('click', () => alTocarAgregar(boton));
  });
}

// ---------- Agregar al carrito ----------

function alTocarAgregar(boton) {
  const pizzaId = Number(boton.dataset.agregar);

  cambiarCantidad(pizzaId, 1);   // suma una pizza al carrito guardado
  actualizarContadorNav();
  actualizarBarraInferior();
  avisarQueSeAgrego(boton);
}

// Por un instante, el botón muestra una tilde para confirmar que se agregó.
function avisarQueSeAgrego(boton) {
  boton.textContent = '✓';
  boton.classList.add('agregado');

  setTimeout(() => {
    boton.textContent = '+';
    boton.classList.remove('agregado');
  }, 700);
}

// La barra de abajo aparece cuando hay al menos una pizza elegida.
function actualizarBarraInferior() {
  const cantidad = contarPizzas(leerCarrito());

  obtener('barra-carrito').hidden = cantidad === 0;
  obtener('barra-texto').textContent =
    cantidad === 1 ? '1 pizza en tu pedido' : cantidad + ' pizzas en tu pedido';
}

// ---------- Inicio ----------
actualizarBarraInferior();
cargarMenu();
