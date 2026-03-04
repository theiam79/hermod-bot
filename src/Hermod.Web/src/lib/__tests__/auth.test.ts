import { describe, it, expect, vi, beforeEach } from 'vitest';
import { get } from 'svelte/store';
import { user, loading, checkAuth } from '$lib/stores/auth';

const mockFetch = vi.fn();
vi.stubGlobal('fetch', mockFetch);

beforeEach(() => {
	mockFetch.mockReset();
	user.set(null);
	loading.set(true);
});

describe('auth store', () => {
	it('starts with null user and loading true', () => {
		expect(get(user)).toBeNull();
		expect(get(loading)).toBe(true);
	});

	it('checkAuth sets user on successful auth', async () => {
		const testUser = { userId: '123', username: 'testuser', avatarUrl: 'https://example.com/avatar.png' };
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(testUser) });

		const result = await checkAuth();

		expect(result).toEqual(testUser);
		expect(get(user)).toEqual(testUser);
		expect(get(loading)).toBe(false);
	});

	it('checkAuth sets user to null on 401', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 401 });

		const result = await checkAuth();

		expect(result).toBeNull();
		expect(get(user)).toBeNull();
		expect(get(loading)).toBe(false);
	});
});
