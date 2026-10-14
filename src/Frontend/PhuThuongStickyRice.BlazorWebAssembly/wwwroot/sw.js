/* Xoi Phu Thuong PWA service worker: caches the app shell on
 * install and serves the offline.html fallback for navigations
 * when the network is unavailable. */
var CACHE = "xoi-phu-thuong-v1";
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

    // App shell assets: cache first, then network.
    event.respondWith(
        caches.match(request).then(function (cached) {
            return cached || fetch(request).then(function (response) {
                var copy = response.clone();
                caches.open(CACHE).then(function (cache) { cache.put(request, copy); });
                return response;
            }).catch(function () { return cached; });
        })
    );
});
