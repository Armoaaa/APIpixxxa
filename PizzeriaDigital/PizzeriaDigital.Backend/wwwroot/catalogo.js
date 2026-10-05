// =====================================================
//  catalogo.js  ->  página index.html
//  Muestra las pizzas y permite agregarlas al carrito.
// =====================================================

let menu = [];


// =====================================================
//  Cargar y mostrar el menú
// =====================================================

async function cargarMenu() {

  try {

    menu = await llamarApi('/pizzas');

    mostrarMenu();

  } catch (error) {

    obtener('grilla-menu').innerHTML =
      '<p class="loading">No pudimos cargar el menu: '
      + error.message +
      '</p>';

  }

}


// =====================================================
//  Mostrar pizzas
// =====================================================

function mostrarMenu() {

  let tarjetas = '';


  menu.forEach((pizza, posicion) => {

    /*
     * El nombre de la pizza viene de la API.
     *
     * Ejemplo:
     * pizza.nombre = "Napolitana"
     *
     * entonces busca:
     * /imagenes/Napolitana.png
     */

    const rutaImagen =
      '/imagenes/' +
      pizza.nombre +
      '.png';


    tarjetas += `

      <article class="pizza-card">

        <span class="pizza-number">
          0${posicion + 1} / ${pizza.tamano}
        </span>


        <!-- ==========================================
             IMAGEN DE LA PIZZA
             ========================================== -->

        <div class="pizza-image">

          <img
            src="${rutaImagen}"
            alt="Pizza ${pizza.nombre}"
            onerror="this.src='/imagenes/Pizza.png'"
          >

        </div>


        <h3>
          ${pizza.nombre}
        </h3>


        <p>
          ${pizza.ingredientes.join(' · ')}
        </p>


        <div class="pizza-bottom">

          <span class="price">
            ${formatearPrecio(pizza.precio)}
          </span>


          <button
            class="add-button"
            type="button"
            aria-label="Agregar ${pizza.nombre}"
            data-agregar="${pizza.id}"
          >
            +
          </button>

        </div>

      </article>

    `;

  });


  obtener('grilla-menu').innerHTML = tarjetas;


  // ===================================================
  //  Eventos de los botones "+"
  // ===================================================

  const botones =
    document.querySelectorAll('[data-agregar]');


  botones.forEach(boton => {

    boton.addEventListener(
      'click',
      () => alTocarAgregar(boton)
    );

  });

}


// =====================================================
//  Agregar al carrito
// =====================================================

function alTocarAgregar(boton) {

  const pizzaId =
    Number(boton.dataset.agregar);


  cambiarCantidad(
    pizzaId,
    1
  );


  actualizarContadorNav();

  actualizarBarraInferior();

  avisarQueSeAgrego(boton);

}


// =====================================================
//  Animación del botón
// =====================================================

function avisarQueSeAgrego(boton) {

  boton.textContent = '✓';

  boton.classList.add('agregado');


  setTimeout(() => {

    boton.textContent = '+';

    boton.classList.remove('agregado');

  }, 700);

}


// =====================================================
//  Barra inferior del carrito
// =====================================================

function actualizarBarraInferior() {

  const cantidad =
    contarPizzas(leerCarrito());


  obtener('barra-carrito').hidden =
    cantidad === 0;


  obtener('barra-texto').textContent =
    cantidad === 1
      ? '1 pizza en tu pedido'
      : cantidad + ' pizzas en tu pedido';

}


// =====================================================
//  Inicio
// =====================================================

actualizarBarraInferior();

cargarMenu();