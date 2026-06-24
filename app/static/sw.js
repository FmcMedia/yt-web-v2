const CACHE = 'yt-web-v2.5';
const PRECACHE = ['/'];

self.addEventListener('install', e => {
  e.waitUntil(caches.open(CACHE).then(c => c.addAll(PRECACHE)));
  self.skipWaiting();
});

self.addEventListener('activate', e => {
  e.waitUntil(
    caches.keys().then(keys =>
      Promise.all(keys.filter(k => k !== CACHE).map(k => caches.delete(k)))
    )
  );
  self.clients.claim();
});

// Network-first: always try live, fall back to cache for the shell only
self.addEventListener('fetch', e => {
  if (e.request.method !== 'GET') return;
  const url = new URL(e.request.url);

  // API calls and file downloads — never cache
  if (url.pathname.startsWith('/api/') ||
      url.pathname.startsWith('/downloads/') ||
      url.pathname.startsWith('/transcripts/')) {
    return;
  }

  e.respondWith(
    fetch(e.request)
      .then(res => {
        if (url.pathname === '/') {
          const clone = res.clone();
          caches.open(CACHE).then(c => c.put(e.request, clone));
        }
        return res;
      })
      .catch(() => caches.match(e.request))
  );
});
