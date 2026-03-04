import { defineConfig } from '@playwright/test';

export default defineConfig({
	webServer: {
		command: 'npm run build && npm run preview -- --port 4173',
		port: 4173
	},
	testDir: 'e2e',
	testMatch: '**/*.test.ts',
	use: {
		baseURL: 'http://localhost:4173'
	}
});
