/* Xoi Huyen PWA service worker: caches the app shell on
 * install and serves the offline.html fallback for navigations
 * when the network is unavailable.
 *
 * NOTE (2026-10-15): asset strategy is network-first with cache fallback.
 * The old cache-first strategy served stale CSS/JS forever after a deploy
 * (cache name was never bumped and sw.js bytes rarely change), so pulling
 * new code + Ctrl+F5 still showed the old theme. Network-first fixes that
 * while keeping the offline fallback working.
 */
var CACHE = "xoi-phu-thuong-v2";
var SHELL = [
    "/",
    "/offline.html",
    "/manifest.json",
    "/css/bootstrap/bootstrap.min.css",
    "/css/app.css"
];

self.addEventListener("install", function (event) {
    event.waitUntil(
        caches.open(CACHE).then(function (cache) {
            return cache.addAll(SHELL);
        }).then(function () { return self.skipWaiting(); })
    );
});

self.addEventListener("activate", function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.filter(function (k) { return k !== CACHE; })
                .map(function (k) { return caches.delete(k); }));
        }).then(function () { return self.clients.claim(); })
    );
});

self.addEventListener("fetch", function (event) {
    var request = event.request;
    if (request.method !== "GET") {
        return;
    }

    // Navigations: network first, offline.html fallback.
    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).catch(function () {
                return caches.match("/offline.html");
            })
        );
        return;
    }

    // App shell assets: network first, cache fallback.
    // Cache-first used to pin stale CSS/JS after deploys; network-first
    // always picks up the latest deploy and still works offline.
    event.respondWith(
        fetch(request).then(function (response) {
            if (response && (response.ok || response.type === "opaque")) {
                var copy = response.clone();
                caches.open(CACHE).then(function (cache) { cache.put(request, copy); });
            }
            return response;
        }).catch(function () {
            return caches.match(request);
        })
    );
});
