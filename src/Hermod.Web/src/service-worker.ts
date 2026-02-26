/// <reference types="@sveltejs/kit" />
/// <reference lib="webworker" />

declare const self: ServiceWorkerGlobalScope;

import { build, files, version } from '$service-worker';

const APP_CACHE = `app-${version}`;
const SHARE_CACHE = 'share-target';

const PRECACHE_ASSETS = [...build, ...files];

self.addEventListener('install', (event) => {
	event.waitUntil(
		caches
			.open(APP_CACHE)
			.then((cache) => cache.addAll(PRECACHE_ASSETS))
			.then(() => self.skipWaiting())
	);
});

self.addEventListener('activate', (event) => {
	event.waitUntil(
		caches.keys().then((keys) =>
			Promise.all(
				keys
					.filter((key) => key !== APP_CACHE && key !== SHARE_CACHE)
					.map((key) => caches.delete(key))
			)
		).then(() => self.clients.claim())
	);
});

self.addEventListener('fetch', (event) => {
	const url = new URL(event.request.url);

	// Intercept share target POST to /upload
	if (url.pathname === '/upload' && event.request.method === 'POST') {
		event.respondWith(handleShareTarget(event.request));
		return;
	}

	// Don't cache API or auth requests
	if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/auth/') || url.pathname.startsWith('/signin-')) {
		return;
	}

	// Cache-first for build assets and static files
	if (PRECACHE_ASSETS.includes(url.pathname)) {
		event.respondWith(
			caches.match(event.request).then((cached) => cached || fetch(event.request))
		);
		return;
	}

	// Network-first for navigation requests (fall back to cached SPA shell)
	if (event.request.mode === 'navigate') {
		event.respondWith(
			fetch(event.request).catch(() =>
				caches.match('/200.html').then((cached) => cached || caches.match('/'))
			).then((response) => response || new Response('Offline', { status: 503 }))
		);
		return;
	}
});

async function handleShareTarget(request: Request): Promise<Response> {
	const formData = await request.formData();
	const file = formData.get('file');

	if (file instanceof File) {
		const cache = await caches.open(SHARE_CACHE);
		await cache.put('/shared-file', new Response(file));
	}

	return Response.redirect('/upload?shared=true', 303);
}
