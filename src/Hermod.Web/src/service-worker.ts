/// <reference lib="webworker" />

declare const self: ServiceWorkerGlobalScope;

const CACHE_NAME = 'share-target';

self.addEventListener('install', () => {
	self.skipWaiting();
});

self.addEventListener('activate', (event) => {
	event.waitUntil(self.clients.claim());
});

self.addEventListener('fetch', (event) => {
	const url = new URL(event.request.url);

	// Intercept share target POST to /upload
	if (url.pathname === '/upload' && event.request.method === 'POST') {
		event.respondWith(handleShareTarget(event.request));
	}
});

async function handleShareTarget(request: Request): Promise<Response> {
	const formData = await request.formData();
	const file = formData.get('file');

	if (file instanceof File) {
		const cache = await caches.open(CACHE_NAME);
		await cache.put('/shared-file', new Response(file));
	}

	return Response.redirect('/upload?shared=true', 303);
}
