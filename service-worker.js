/**
 * service-worker.js — مدار 2
 * استراتيجية التخزين المؤقت لدعم وضع عدم الاتصال
 *
 * استراتيجيتان:
 *   Cache First  → الملفات الثابتة (HTML/JS/CSS)
 *   Network First + Cache Fallback → طلبات API
 */

const STATIC_CACHE  = 'mdar-static-v1';
const DATA_CACHE    = 'mdar-data-v1';
const CACHE_VERSION = 1;

// الملفات الثابتة التي تُخزَّن عند أول تحميل
const STATIC_FILES = [
  '/InfiniteCanvas.html',
  '/dashboard.html',
  '/SyncController.js',
  '/OfflineStore.js',
  // Google Fonts — تُخزَّن عند أول طلب (Stale While Revalidate)
  'https://fonts.googleapis.com/css2?family=Cairo:wght@400;600&family=Amiri:ital,wght@0,400;0,700;1,400&family=Scheherazade+New:wght@400;700&display=swap',
];

// ── Install: تخزين الملفات الثابتة ─────────────────────────────────────────
self.addEventListener('install', event => {
  event.waitUntil(
    caches.open(STATIC_CACHE)
      .then(cache => cache.addAll(
        STATIC_FILES.filter(f => !f.startsWith('http'))  // الملفات المحلية فقط
      ))
      .then(() => self.skipWaiting())
  );
});

// ── Activate: تنظيف الـ caches القديمة ─────────────────────────────────────
self.addEventListener('activate', event => {
  event.waitUntil(
    caches.keys()
      .then(keys => Promise.all(
        keys
          .filter(key => key !== STATIC_CACHE && key !== DATA_CACHE)
          .map(key  => caches.delete(key))
      ))
      .then(() => self.clients.claim())
  );
});

// ── Fetch: توجيه الطلبات ───────────────────────────────────────────────────
self.addEventListener('fetch', event => {
  const { request } = event;
  const url = new URL(request.url);

  // طلبات API → Network First + Cache Fallback
  if (url.pathname.startsWith('/api/')) {
    event.respondWith(networkFirstWithFallback(request));
    return;
  }

  // SignalR WebSocket → تجاهل (لا يُخزَّن)
  if (url.pathname.startsWith('/hubs/')) return;

  // الملفات الثابتة → Cache First
  event.respondWith(cacheFirst(request));
});

// ── Cache First ──────────────────────────────────────────────────────────────
async function cacheFirst(request) {
  const cached = await caches.match(request);
  if (cached) return cached;

  try {
    const response = await fetch(request);
    if (response.ok) {
      const cache = await caches.open(STATIC_CACHE);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    // لا شبكة ولا cache — أعد صفحة offline بسيطة
    return offlineFallback(request);
  }
}

// ── Network First + Cache Fallback ───────────────────────────────────────────
async function networkFirstWithFallback(request) {
  try {
    const response = await fetch(request);
    if (response.ok && request.method === 'GET') {
      const cache = await caches.open(DATA_CACHE);
      cache.put(request, response.clone());
    }
    return response;
  } catch {
    // فشل الشبكة — أعد آخر نسخة مخزّنة
    const cached = await caches.match(request);
    if (cached) return cached;

    // لا يوجد شيء — أعد استجابة JSON تشير للـ offline
    return new Response(
      JSON.stringify({ offline: true, message: 'لا يوجد اتصال — يعمل مدار في وضع عدم الاتصال' }),
      {
        status:  503,
        headers: { 'Content-Type': 'application/json' },
      }
    );
  }
}

// ── Offline Fallback Page ────────────────────────────────────────────────────
function offlineFallback(request) {
  if (request.destination === 'document') {
    return caches.match('/InfiniteCanvas.html')
      || new Response('<h1 dir="rtl">مدار — لا يوجد اتصال</h1>', {
           headers: { 'Content-Type': 'text/html; charset=utf-8' },
         });
  }
  return new Response('', { status: 503 });
}

// ── Background Sync ──────────────────────────────────────────────────────────
// يُطلق عند عودة الإنترنت (إذا دعم المتصفح Background Sync API)
self.addEventListener('sync', event => {
  if (event.tag === 'mdar-sync-pending') {
    event.waitUntil(
      // إشعار الـ clients بأن الإنترنت عاد
      self.clients.matchAll().then(clients =>
        clients.forEach(client =>
          client.postMessage({ type: 'SYNC_PENDING' })
        )
      )
    );
  }
});
