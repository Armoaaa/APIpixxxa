// =====================================================
// catalogo.js
//
// Página index.html
//
// Las pizzas NO están escritas manualmente acá.
//
// El menú se obtiene desde:
//
// MySQL
//   ↓
// PizzaRepository
//   ↓
// PizzaController
//   ↓
// GET /api/pizzas
//   ↓
// Este archivo
//
// Por lo tanto, cualquier pizza nueva agregada
// a la base de datos aparecerá automáticamente.
// =====================================================


// =====================================================
// MENÚ
// =====================================================

let menu = [];


// =====================================================
// CARGAR MENÚ DESDE LA API
// =====================================================

async function cargarMenu() {

  const grilla = obtener('grilla-menu');


  try {

    // ================================================
    // Pedimos las pizzas directamente al Backend.
    //
    // GET /api/pizzas
    // ================================================

    menu = await llamarApi('/pizzas');


    // Nos aseguramos de que realmente recibimos
    // una lista.

    if (!Array.isArray(menu)) {

      throw new Error(
        'La API no devolvio una lista de pizzas.'
      );

    }


    // ================================================
    // Actualizar cantidad de pizzas
    // ================================================

    actualizarCantidadPizzas();


    // ================================================
    // Mostrar pizzas
    // ================================================

    mostrarMenu();


  } catch (error) {

    console.error(
      'Error cargando el menu:',
      error
    );


    grilla.innerHTML = `

      <p class="loading">

        No pudimos cargar el menu:

        ${escapeHtml(error.message)}

      </p>

    `;


    // Si no se pudo cargar, mostramos 0.

    actualizarCantidadPizzas();

  }

}



// =====================================================
// ACTUALIZAR CANTIDAD DE PIZZAS
// =====================================================
//
// Esto modifica:
//
// <strong id="cantidad-pizzas">
//
// de index.html.
//
// Antes estaba escrito manualmente como:
//
// 5
//
// Ahora viene directamente de la BD.
// =====================================================

function actualizarCantidadPizzas() {

  const elemento =
    obtener('cantidad-pizzas');


  if (!elemento) {
    return;
  }


  elemento.textContent =
    menu.length;

}



// =====================================================
// MOSTRAR MENÚ
// =====================================================

function mostrarMenu() {

  const grilla =
    obtener('grilla-menu');


  // ================================================
  // Si la base de datos no tiene pizzas
  // ================================================

  if (menu.length === 0) {

    grilla.innerHTML = `

      <p class="loading">

        No hay pizzas disponibles
        en este momento.

      </p>

    `;

    return;

  }


  let tarjetas = '';



  // =================================================
  // Crear una tarjeta por cada pizza
  // =================================================

  menu.forEach((pizza, posicion) => {


    // ================================================
    // Imagen
    // ================================================
    //
    // Si en BD tenemos:
    //
    // Nombre = "Napolitana"
    //
    // busca:
    //
    // /imagenes/Napolitana.png
    //
    // Si no existe, usa Pizza.png.
    // ================================================

    const rutaImagen =
      '/imagenes/' +
      encodeURIComponent(pizza.nombre) +
      '.png';



    // ================================================
    // Ingredientes
    // ================================================

    let ingredientes = '';


    if (
      Array.isArray(pizza.ingredientes) &&
      pizza.ingredientes.length > 0
    ) {

      ingredientes =
        pizza.ingredientes
          .map(ingrediente =>
            escapeHtml(ingrediente)
          )
          .join(' · ');

    } else {

      ingredientes =
        'Sin ingredientes especificados';

    }



    // ================================================
    // Numero visual de la tarjeta
    // ================================================

    const numero =
      String(posicion + 1).padStart(2, '0');



    // ================================================
    // Crear tarjeta
    // ================================================

    tarjetas += `

      <article class="pizza-card">


        <span class="pizza-number">

          ${numero} / ${escapeHtml(pizza.tamano)}

        </span>



        <!-- ==========================================
             IMAGEN
             ========================================== -->

        <div class="pizza-image">

          <img
            src="${rutaImagen}"
            alt="Pizza ${escapeHtml(pizza.nombre)}"
            onerror="this.onerror=null; this.src='/imagenes/Pizza.png';"
          >

        </div>



        <!-- ==========================================
             NOMBRE
             ========================================== -->

        <h3>

          ${escapeHtml(pizza.nombre)}

        </h3>



        <!-- ==========================================
             INGREDIENTES
             ========================================== -->

        <p>

          ${ingredientes}

        </p>



        <!-- ==========================================
             PRECIO Y BOTON
             ========================================== -->

        <div class="pizza-bottom">


          <span class="price">

            ${formatearPrecio(pizza.precio)}

          </span>



          <button
            class="add-button"
            type="button"
            aria-label="Agregar ${escapeHtml(pizza.nombre)}"
            data-agregar="${pizza.id}"
          >

            +

          </button>


        </div>

      </article>

    `;

  });



  // =================================================
  // Insertar tarjetas en la página
  // =================================================

  grilla.innerHTML =
    tarjetas;



  // =================================================
  // Eventos de los botones "+"
  // =================================================

  const botones =
    document.querySelectorAll(
      '[data-agregar]'
    );


  botones.forEach(boton => {

    boton.addEventListener(
      'click',
      () => alTocarAgregar(boton)
    );

  });

}



// =====================================================
// ESCAPAR TEXTO HTML
// =====================================================
//
// Esto evita que un nombre o ingrediente guardado
// en la BD pueda romper el HTML.
// =====================================================

function escapeHtml(valor) {

  const texto =
    String(valor ?? '');


  return texto
    .replace(/&/g, '&amp;')
    .replace(/</g, '&lt;')
    .replace(/>/g, '&gt;')
    .replace(/"/g, '&quot;')
    .replace(/'/g, '&#039;');

}



// =====================================================
// AGREGAR PIZZA AL CARRITO
// =====================================================

function alTocarAgregar(boton) {


  const pizzaId =
    Number(
      boton.dataset.agregar
    );


  // ================================================
  // El ID utilizado es el ID de la BD.
  // ================================================

  cambiarCantidad(
    pizzaId,
    1
  );


  actualizarContadorNav();


  actualizarBarraInferior();


  avisarQueSeAgrego(
    boton
  );

}



// =====================================================
// ANIMACIÓN DEL BOTÓN
// =====================================================

function avisarQueSeAgrego(boton) {


  boton.textContent =
    '✓';


  boton.classList.add(
    'agregado'
  );


  setTimeout(() => {

    boton.textContent =
      '+';


    boton.classList.remove(
      'agregado'
    );

  }, 700);

}



// =====================================================
// BARRA INFERIOR DEL CARRITO
// =====================================================

function actualizarBarraInferior() {


  const cantidad =
    contarPizzas(
      leerCarrito()
    );


  const barra =
    obtener('barra-carrito');


  const texto =
    obtener('barra-texto');


  if (!barra || !texto) {
    return;
  }


  // Mostrar u ocultar barra.

  barra.hidden =
    cantidad === 0;


  // Texto.

  texto.textContent =
    cantidad === 1
      ? '1 pizza en tu pedido'
      : cantidad + ' pizzas en tu pedido';

}



// =====================================================
// INICIO
// =====================================================

async function iniciarCatalogo() {


  actualizarBarraInferior();


  await cargarMenu();

}



// =====================================================
// EJECUTAR
// =====================================================

iniciarCatalogo();