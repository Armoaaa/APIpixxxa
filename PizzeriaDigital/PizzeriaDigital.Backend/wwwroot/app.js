const api = '/api';
let menu = [];
let cart = [];
let customerId = null;

const money = value => new Intl.NumberFormat('es-AR', { style: 'currency', currency: 'ARS', maximumFractionDigits: 0 }).format(value);
const byId = id => document.getElementById(id);

async function request(path, options = {}) {
  const response = await fetch(`${api}${path}`, { headers: { 'Content-Type': 'application/json' }, ...options });
  const body = await response.json().catch(() => null);
  if (!response.ok) throw new Error(body?.mensaje || `No se pudo completar la operacion (${response.status}).`);
  return body;
}

function renderMenu() {
  byId('menu-grid').innerHTML = menu.map((pizza, index) => `
    <article class="pizza-card">
      <span class="pizza-number">0${index + 1} / ${pizza.tamano}</span>
      <div class="pizza-badge">${pizza.ingredientes[0] || 'Pizza'}</div>
      <h3>${pizza.nombre}</h3>
      <p>${pizza.ingredientes.join(' · ')}</p>
      <div class="pizza-bottom"><span class="price">${money(pizza.precio)}</span><button class="add-button" type="button" aria-label="Agregar ${pizza.nombre}" data-add="${pizza.id}">+</button></div>
    </article>`).join('');
  document.querySelectorAll('[data-add]').forEach(button => button.addEventListener('click', () => addToCart(Number(button.dataset.add))));
}

function addToCart(pizzaId) {
  const item = cart.find(entry => entry.pizzaId === pizzaId);
  if (item) item.cantidad += 1;
  else cart.push({ pizzaId, cantidad: 1 });
  renderCart();
  byId('pedido').scrollIntoView({ behavior: 'smooth', block: 'start' });
}

function renderCart() {
  const count = cart.reduce((sum, item) => sum + item.cantidad, 0);
  const total = cart.reduce((sum, item) => {
    const pizza = menu.find(p => p.id === item.pizzaId);
    return sum + (pizza ? pizza.precio * item.cantidad : 0);
  }, 0);
  byId('cart-count').textContent = count;
  byId('cart-total').textContent = money(total);
  byId('place-order').disabled = count === 0 || !customerId;
  byId('cart-items').innerHTML = cart.length ? cart.map(item => {
    const pizza = menu.find(p => p.id === item.pizzaId);
    return `<div class="cart-row"><strong>${pizza.nombre}</strong><small>${item.cantidad} x ${money(pizza.precio)}</small><div class="cart-actions"><button type="button" aria-label="Quitar una unidad" data-remove="${pizza.id}">−</button><span>${item.cantidad}</span><button type="button" aria-label="Agregar una unidad" data-add-cart="${pizza.id}">+</button></div></div>`;
  }).join('') : '<p class="empty-state">Todavia no agregaste pizzas.<br><a href="#menu">Volver al menu →</a></p>';
  document.querySelectorAll('[data-remove]').forEach(button => button.addEventListener('click', () => updateCart(Number(button.dataset.remove), -1)));
  document.querySelectorAll('[data-add-cart]').forEach(button => button.addEventListener('click', () => updateCart(Number(button.dataset.addCart), 1)));
}

function updateCart(pizzaId, amount) {
  const item = cart.find(entry => entry.pizzaId === pizzaId);
  if (!item) return;
  item.cantidad += amount;
  if (item.cantidad <= 0) cart = cart.filter(entry => entry.pizzaId !== pizzaId);
  renderCart();
}

byId('customer-form').addEventListener('submit', async event => {
  event.preventDefault();
  const result = byId('customer-result');
  result.textContent = 'Guardando tus datos...';
  try {
    const customer = await request('/clientes', { method: 'POST', body: JSON.stringify({
      nombre: byId('customer-name').value.trim(), telefono: byId('customer-phone').value.trim(), direccion: byId('customer-address').value.trim()
    }) });
    customerId = customer.id;
    result.textContent = `Cliente #${customer.id} listo. Ya podes confirmar el pedido.`;
    result.style.color = '#52735a';
    renderCart();
  } catch (error) { result.textContent = error.message; result.style.color = ''; }
});

byId('place-order').addEventListener('click', async () => {
  const result = byId('order-result');
  result.textContent = 'Enviando pedido a la cocina...';
  try {
    const order = await request('/pedidos', { method: 'POST', body: JSON.stringify({ clienteId: customerId, items: cart }) });
    result.textContent = `Pedido #${order.id} recibido. Estado: ${stateName(order.estado)}.`;
    result.style.color = '#52735a';
    byId('track-id').value = order.id;
    renderTracking(order);
    byId('seguimiento').scrollIntoView({ behavior: 'smooth', block: 'start' });
    cart = [];
    renderCart();
  } catch (error) { result.textContent = error.message; result.style.color = ''; }
});

byId('track-form').addEventListener('submit', async event => {
  event.preventDefault();
  const result = byId('tracking-result');
  result.innerHTML = '<span class="tracking-icon">...</span><span>Buscando tu pedido...</span>';
  try { renderTracking(await request(`/pedidos/${Number(byId('track-id').value)}`)); }
  catch (error) { result.innerHTML = `<span class="tracking-icon">!</span><span>${error.message}</span>`; }
});

byId('client-lookup-form').addEventListener('submit', async event => {
  event.preventDefault();
  const result = byId('client-result');
  try { const customer = await request(`/clientes/${Number(byId('client-id').value)}`); result.textContent = `${customer.nombre} · ${customer.direccion}`; result.style.color = '#52735a'; }
  catch (error) { result.textContent = error.message; result.style.color = ''; }
});

function stateName(value) {
  return ['Espera de confirmacion', 'En preparacion', 'En viaje', 'Entregado'][value] || 'Estado desconocido';
}

function renderTracking(order) {
  const result = byId('tracking-result');
  const state = stateName(order.estado);
  const detail = order.conError ? `Problema: ${order.ultimoError}` : `Pedido #${order.id} · ${money(order.total)}`;
  result.innerHTML = `<span class="tracking-icon">${order.conError ? '!' : '✓'}</span><span><strong>${state}</strong>${detail}</span>`;
  result.style.background = order.conError ? '#f4d5cb' : '';
}

async function loadMenu() {
  try { menu = await request('/pizzas'); renderMenu(); }
  catch (error) { byId('menu-grid').innerHTML = `<p class="loading">No pudimos cargar el menu: ${error.message}</p>`; }
}

loadMenu();
