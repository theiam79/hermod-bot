import { test, expect } from '@playwright/test';

test('landing page shows login button when unauthenticated', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('button', { name: /sign in with discord/i })).toBeVisible();
});

test('landing page shows Hermod heading', async ({ page }) => {
	await page.goto('/');
	await expect(page.getByRole('heading', { name: 'Hermod' })).toBeVisible();
});

test('unauthenticated user sees login prompt on upload page', async ({ page }) => {
	await page.goto('/upload');
	await expect(page.getByText(/sign in to upload/i)).toBeVisible();
});

test('unauthenticated user sees login prompt on plays page', async ({ page }) => {
	await page.goto('/plays');
	await expect(page.getByText(/sign in to view/i)).toBeVisible();
});

test('unauthenticated user sees login prompt on groups page', async ({ page }) => {
	await page.goto('/groups');
	await expect(page.getByText(/sign in to view/i)).toBeVisible();
});
