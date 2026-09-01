const CACHE_NAME = "v20260801";
const BASE = self.registration.scope;
const ASSETS = [
    "",
    "index.html",
    "main.js",
    "dashboard.html",
    "assets/logo.webp",
    "favicon.webp"
].map(p => new URL(p, BASE).href);

const addResourcesToCache = async (resources) => {
    const cache = await caches.open(CACHE_NAME);
    await cache.addAll(resources);
};

const putInCache = async (request, response) => {
    const cache = await caches.open(CACHE_NAME);
    await cache.put(request, response);
};

const generateErrorResponse = () => new Response("Network error happened", {
    status: 408,
    headers: { "Content-Type": "text/plain" },
});

const cacheFirst = async ({request, preloadResponse, event}) => {
    if (request.method !== 'GET' || request.url.startsWith('chrome-extension://')) {
        return fetch(request);
    }
    // First try to get the resource from the cache
    const responseFromCache = await caches.match(request);
    if (responseFromCache) {
        return responseFromCache;
    }

    // for navigation requests, fallback to cached index.html
    if (request.mode === 'navigate') {
        let requestUrl = request.url.includes('dashboard.html') ? new URL("dashboard.html", BASE).href : new URL("index.html", BASE).href;
        let cachedIndex = await caches.match(requestUrl);
        if (cachedIndex) {
            return cachedIndex;
        }
        await addResourcesToCache(ASSETS);
        cachedIndex = await caches.match(requestUrl);
        if (cachedIndex) {
            return cachedIndex;
        }
        return generateErrorResponse();
    }

    // Next try to use (and cache) the preloaded response, if it's there
    if (preloadResponse) {
        event.waitUntil(putInCache(request, preloadResponse.clone()));
        return preloadResponse;
    }

    // Next try to get the resource from the network
    try {
        const responseFromNetwork = await fetch(request);
        // response may be used only once
        // we need to save clone to put one copy in cache
        // and serve second one
        if (!responseFromNetwork.redirected) {
            event.waitUntil(putInCache(request, responseFromNetwork.clone()));
        }
        return responseFromNetwork;
    } catch (error) {
        if (request.mode === 'navigate') {
            let requestUrl = request.url.includes('dashboard.html') ? new URL("dashboard.html", BASE).href : new URL("index.html", BASE).href;
            const cachedIndex = await caches.match(requestUrl);
            if (cachedIndex) {
                return cachedIndex;
            }
        }
        return generateErrorResponse();
    }
};

// Enable navigation preload
const enableNavigationPreloadAndClearOldCache = async () => {
    if (self.registration.navigationPreload) {
        try {
            await self.registration.navigationPreload.enable();
        }
        catch (error) {
            console.error("Error enabling navigation preload", error);
        }
    }
    caches.keys().then(keys => Promise.all(keys.filter(k => k !== CACHE_NAME).map(k => caches.delete(k))))
};

self.addEventListener("activate", (event) => {
    event.waitUntil(enableNavigationPreloadAndClearOldCache());
    self.clients.claim();
});
self.addEventListener("install", (event) => {
    event.waitUntil(addResourcesToCache(ASSETS));
    self.skipWaiting();
});
self.addEventListener("fetch", (event) => {
    if (event.request.method !== "GET") {
        return;
    }
    const preloadPromise = event.preloadResponse;
    event.respondWith((async () => {
        const preloadResponse = await preloadPromise;
        return cacheFirst({ request: event.request, preloadResponse, event});
    })());
    if (preloadPromise) {
        event.waitUntil(preloadPromise);
    }
});