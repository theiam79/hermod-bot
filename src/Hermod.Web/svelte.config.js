import adapter from '@sveltejs/adapter-static';

/** @type {import('@sveltejs/kit').Config} */
const config = {
	kit: {
		adapter: adapter({
			pages: 'dist',
			assets: 'dist',
			fallback: 'index.html'
		}),
		serviceWorker: {
			files: (filepath) => !filepath.startsWith('/screenshots')
		}
	}
};

export default config;
