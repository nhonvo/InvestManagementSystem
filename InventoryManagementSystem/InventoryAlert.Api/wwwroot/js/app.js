/**
 * app.js — InventoryAlert Dashboard
 * Vanilla JS, no framework. ~120 lines.
 */

const API = {
  products:  '/api/products',
  alerts:    '/api/products/price-alerts',
  syncPrice: '/api/products/sync-price',
};

// ── State ──────────────────────────────────────────────────────────────────
let products = [];
let alerts   = [];

// ── Utils ──────────────────────────────────────────────────────────────────
const el  = (id) => document.getElementById(id);
const $   = (sel) => document.querySelector(sel);
const fmt = {
  money: (v) => v == null ? '—' : Number(v).toFixed(2),
  pct:   (v) => v == null ? '—' : `${(Number(v) * 100).toFixed(1)}%`,
  time:  ()  => new Date().toLocaleTimeString(),
};

async function get(url) {
  const r = await fetch(url, { headers: { Accept: 'application/json' } });
  if (!r.ok) throw new Error(`HTTP ${r.status}`);
  return r.status === 204 ? null : r.json();
}

// ── Navigation ─────────────────────────────────────────────────────────────
document.querySelectorAll('.nav-link').forEach((link) => {
  link.addEventListener('click', (e) => {
    e.preventDefault();
    const view = link.dataset.view;
    document.querySelectorAll('.view').forEach((v) => v.classList.remove('active'));
    document.querySelectorAll('.nav-link').forEach((l) => l.classList.remove('active'));
    el(`view-${view}`).classList.add('active');
    link.classList.add('active');
  });
});

// ── Render helpers ─────────────────────────────────────────────────────────
function renderProducts(data) {
  const tbody = el('tbody-products');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="8" class="empty">No products found.</td></tr>';
    return;
  }
  tbody.innerHTML = data.map((p) => {
    const low  = p.stockAlertThreshold > 0 && p.stockCount <= p.stockAlertThreshold;
    const drop = p.currentPrice > 0 && p.originPrice > 0 &&
                 (p.originPrice - p.currentPrice) / p.originPrice >= p.priceAlertThreshold;

    const tag = drop
      ? '<span class="tag tag-red">Price Alert</span>'
      : low
        ? '<span class="tag tag-amber">Low Stock</span>'
        : '<span class="tag tag-green">OK</span>';

    return `<tr>
      <td>${p.id}</td>
      <td>${p.name}</td>
      <td>${p.tickerSymbol || '—'}</td>
      <td>${p.stockCount}</td>
      <td>${fmt.money(p.originPrice)}</td>
      <td>${fmt.money(p.currentPrice)}</td>
      <td>${fmt.pct(p.priceAlertThreshold)}</td>
      <td>${tag}</td>
    </tr>`;
  }).join('');
}

function renderAlerts(data) {
  const badge = el('alert-badge');
  badge.textContent = data.length;
  badge.style.display = data.length ? 'inline-block' : 'none';

  const tbody = el('tbody-alerts');
  if (!data.length) {
    tbody.innerHTML = '<tr><td colspan="6" class="empty">No active price alerts.</td></tr>';
    return;
  }
  tbody.innerHTML = data.map((a) => `<tr>
    <td><strong>${a.tickerSymbol}</strong></td>
    <td>${a.name}</td>
    <td>${fmt.money(a.originPrice)}</td>
    <td>${fmt.money(a.currentPrice)}</td>
    <td style="color:#dc2626">−${Math.abs(a.priceChangePercent * 100).toFixed(1)}%</td>
    <td>${fmt.pct(a.priceAlertThreshold)}</td>
  </tr>`).join('');
}

function renderKpis() {
  const low = products.filter(
    (p) => p.stockAlertThreshold > 0 && p.stockCount <= p.stockAlertThreshold
  ).length;

  el('kpi-total').textContent   = products.length;
  el('kpi-alerts').textContent  = alerts.length;
  el('kpi-low').textContent     = low;
  el('kpi-healthy').textContent = Math.max(0, products.length - alerts.length - low);

  const banner = el('alert-banner');
  if (alerts.length) {
    const names = alerts.slice(0, 3).map((a) => a.tickerSymbol).join(', ');
    el('alert-banner-text').textContent =
      `${names}${alerts.length > 3 ? ` +${alerts.length - 3} more` : ''}`;
    banner.style.display = 'block';
  } else {
    banner.style.display = 'none';
  }
}

// ── Load data ──────────────────────────────────────────────────────────────
async function load() {
  try {
    [products, alerts] = await Promise.all([get(API.products), get(API.alerts)]);
    renderKpis();
    renderProducts(products);
    renderAlerts(alerts);
    el('last-updated').textContent = fmt.time();
    el('status-dot').className = 'status-dot online';
  } catch {
    el('status-dot').className = 'status-dot offline';
    el('last-updated').textContent = `Failed at ${fmt.time()}`;
  }
}

// ── Search ─────────────────────────────────────────────────────────────────
el('search').addEventListener('input', (e) => {
  const q = e.target.value.toLowerCase().trim();
  renderProducts(products.filter(
    (p) => p.name.toLowerCase().includes(q) || (p.tickerSymbol || '').toLowerCase().includes(q)
  ));
});

// ── Refresh button ─────────────────────────────────────────────────────────
el('refresh-btn').addEventListener('click', async () => {
  el('refresh-btn').textContent = '↻ …';
  el('refresh-btn').disabled = true;
  await load();
  el('refresh-btn').textContent = '↻ Refresh';
  el('refresh-btn').disabled = false;
});

// ── Sync button ────────────────────────────────────────────────────────────
el('sync-btn').addEventListener('click', async () => {
  const btn = el('sync-btn');
  const res = el('sync-result');
  btn.disabled = true;
  btn.textContent = 'Syncing…';
  res.textContent = '';
  try {
    await fetch(API.syncPrice, { method: 'POST' });
    res.textContent = `✓ Sync triggered at ${fmt.time()}`;
    await load();
  } catch (err) {
    res.textContent = `✗ Failed — ${err.message}`;
  } finally {
    btn.disabled = false;
    btn.textContent = 'Trigger Sync';
  }
});

// ── Boot ───────────────────────────────────────────────────────────────────
load();
setInterval(load, 60_000);
