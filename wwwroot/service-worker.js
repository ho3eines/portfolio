/**
 * Portfolio PWA — Service Worker
 * Caches static assets for offline access & fast load
 * Version: 2026.08.04
 */
const CACHE_NAME = 'portfolio-v20260806-redesign';
const RUNTIME = 'portfolio-runtime';

// Assets to pre-cache on install
const PRECACHE_URLS = [
  '/',
  '/index.html',
  '/css/styles.css',
  '/js/config.js',
  '/js/animations.js',
  '/manifest.json',
  '/images/icon-192.png',
  '/images/icon-512.png',
  'https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500;600;700;800;900&family=Playfair+Display:ital,wght@0,400;0,700;1,400;1,700&display=swap'
];

// ===== INSTALL =====
self.addEventListener('install', (event) => {
  event.waitUntil(
    caches.open(CACHE_NAME)
      .then((cache) => {
        console.log('⚡ PWA: Pre-caching assets...');
        return cache.addAll(PRECACHE_URLS);
      })
      .then(() => self.skipWaiting())
  );
});

// ===== ACTIVATE =====
self.addEventListener('activate', (event) => {
  const currentCaches = [CACHE_NAME, RUNTIME];
  event.waitUntil(
    caches.keys().then((cacheNames) => {
      return Promise.all(
        cacheNames
          .filter((name) => !currentCaches.includes(name))
          .map((name) => {
            console.log('🗑 PWA: Removing old cache:', name);
            return caches.delete(name);
          })
      );
    }).then(() => self.clients.claim())
  );
});

// ===== FETCH — Cache-first with network fallback =====
self.addEventListener('fetch', (event) => {
  // Skip cross-origin requests
  if (!event.request.url.startsWith(self.location.origin) &&
      !event.request.url.includes('fonts.googleapis.com') &&
      !event.request.url.includes('fonts.gstatic.com') &&
      !event.request.url.includes('cdn.jsdelivr.net')) {
    return;
  }

  // Skip API calls
  if (event.request.url.includes('/api/')) return;

  // For navigation requests, serve index.html (SPA fallback)
  if (event.request.mode === 'navigate') {
    event.respondWith(
      caches.match('/index.html')
        .then((cached) => cached || fetch(event.request))
        .catch(() => caches.match('/index.html'))
    );
    return;
  }

  // Cache-first for static assets
  event.respondWith(
    caches.match(event.request).then((cachedResponse) => {
      if (cachedResponse) {
        // Return cached, but update in background
        const fetchPromise = fetch(event.request).then((networkResponse) => {
          if (networkResponse && networkResponse.status === 200) {
            const responseClone = networkResponse.clone();
            caches.open(RUNTIME).then((cache) => {
              cache.put(event.request, responseClone);
            });
          }
          return networkResponse;
        }).catch(() => cachedResponse);
        return cachedResponse;
      }

      // Not in cache — fetch from network
      return fetch(event.request).then((networkResponse) => {
        if (networkResponse && networkResponse.status === 200) {
          const responseClone = networkResponse.clone();
          caches.open(RUNTIME).then((cache) => {
            cache.put(event.request, responseClone);
          });
        }
        return networkResponse;
      }).catch(() => {
        // Offline fallback for images
        if (event.request.destination === 'image') {
          return new Response(
            '<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 200 200"><rect fill="#1a1a1a" width="200" height="200"/><text fill="#d4a04c" x="100" y="105" text-anchor="middle" font-size="48">M</text></svg>',
            { headers: { 'Content-Type': 'image/svg+xml' } }
          );
        }
      });
    })
  );
});

// ===== PUSH NOTIFICATIONS (placeholder) =====
self.addEventListener('push', (event) => {
  const data = event.data ? event.data.json() : {};
  const title = data.title || 'Mahbod Pour Portfolio';
  const options = {
    body: data.body || 'Something new has been added!',
    icon: '/images/icon-192.png',
    badge: '/images/icon-192.png',
    data: { url: data.url || '/' }
  };
  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();
  event.waitUntil(
    clients.matchAll({ type: 'window' }).then((clientList) => {
      for (const client of clientList) {
        if (client.url === event.notification.data.url && 'focus' in client) {
          return client.focus();
        }
      }
      if (clients.openWindow) {
        return clients.openWindow(event.notification.data.url || '/');
      }
    })
  );
});
