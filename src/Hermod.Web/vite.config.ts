import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config';

// Aspire passes the API URL via service discovery env vars
const apiUrl =
	process.env['services__hermod-api__https__0'] ??
	process.env['services__hermod-api__http__0'] ??
	'https://localhost:7007';

export default defineConfig({
	plugins: [sveltekit()],
	server: {
		port: parseInt(process.env.PORT ?? '5173'),
		strictPort: true,
		host: true,
		allowedHosts: true,
		proxy: {
			'/api': {
				target: apiUrl,
				changeOrigin: false,
				secure: false
			},
			'/auth': {
				target: apiUrl,
				changeOrigin: false,
				secure: false
			},
			'/signin-discord': {
				target: apiUrl,
				changeOrigin: false,
				secure: false
			}
		}
	},
	test: {
		include: ['src/**/*.{test,spec}.{js,ts}'],
		environment: 'jsdom'
	}
});
