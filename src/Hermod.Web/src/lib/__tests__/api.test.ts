import { describe, it, expect, vi, beforeEach } from 'vitest';
import { fetchUser, fetchPlays, fetchGroups, uploadPlayFile, login, fetchProfile, updateProfile } from '$lib/api';

const mockFetch = vi.fn();
vi.stubGlobal('fetch', mockFetch);

beforeEach(() => {
	mockFetch.mockReset();
});

describe('fetchUser', () => {
	it('returns null on 401', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 401 });
		const result = await fetchUser();
		expect(result).toBeNull();
	});

	it('returns user on 200', async () => {
		const user = { userId: '123', username: 'testuser', avatarUrl: null };
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(user) });
		const result = await fetchUser();
		expect(result).toEqual(user);
		expect(mockFetch).toHaveBeenCalledWith('/auth/me');
	});
});

describe('fetchPlays', () => {
	it('returns empty response on error', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 500 });
		const result = await fetchPlays(1, 20);
		expect(result.plays).toEqual([]);
		expect(result.totalCount).toBe(0);
	});

	it('returns plays on success', async () => {
		const response = {
			plays: [{ id: '1', gameName: 'Catan', datePlayed: '2025-01-01', duration: '1h 30m', locationName: null, playerCount: 4, hasWinner: true }],
			totalCount: 1,
			page: 1,
			pageSize: 20
		};
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(response) });
		const result = await fetchPlays(1, 20);
		expect(result.plays).toHaveLength(1);
		expect(result.plays[0].gameName).toBe('Catan');
		expect(mockFetch).toHaveBeenCalledWith('/api/plays?page=1&pageSize=20');
	});
});

describe('fetchGroups', () => {
	it('returns empty array on error', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 500 });
		const result = await fetchGroups();
		expect(result).toEqual([]);
	});

	it('returns groups on success', async () => {
		const groups = [{ id: '1', name: 'Test Group', allowSharing: true }];
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(groups) });
		const result = await fetchGroups();
		expect(result).toHaveLength(1);
		expect(result[0].name).toBe('Test Group');
	});
});

describe('uploadPlayFile', () => {
	it('returns error on 401', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 401, text: () => Promise.resolve('Unauthorized') });
		const file = new File(['content'], 'test.bgsplay');
		const result = await uploadPlayFile(file);
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error).toContain('Not authenticated');
	});

	it('returns result on success', async () => {
		const uploadResult = { uploadId: 'abc', playCount: 3 };
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(uploadResult) });
		const file = new File(['content'], 'test.bgsplay');
		const result = await uploadPlayFile(file);
		expect(result.ok).toBe(true);
		if (result.ok) {
			expect(result.result.playCount).toBe(3);
		}
	});

	it('returns error message on bad request', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 400, text: () => Promise.resolve('File exceeds maximum size') });
		const file = new File(['content'], 'test.bgsplay');
		const result = await uploadPlayFile(file);
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error).toContain('File exceeds maximum size');
	});
});

describe('fetchProfile', () => {
	it('returns null on error', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 404 });
		const result = await fetchProfile();
		expect(result).toBeNull();
	});

	it('returns profile on success', async () => {
		const profile = {
			userId: '123',
			displayName: 'Test',
			bggId: 42,
			bggUsername: 'testbgg',
			subscribeToPlays: true,
			groups: [{ groupId: 'g1', name: 'Group 1' }]
		};
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(profile) });
		const result = await fetchProfile();
		expect(result).toEqual(profile);
		expect(mockFetch).toHaveBeenCalledWith('/api/profile');
	});
});

describe('updateProfile', () => {
	it('returns profile on success', async () => {
		const profile = {
			userId: '123',
			displayName: 'Updated',
			bggId: null,
			bggUsername: null,
			subscribeToPlays: true,
			groups: []
		};
		mockFetch.mockResolvedValue({ ok: true, json: () => Promise.resolve(profile) });
		const result = await updateProfile({ displayName: 'Updated' });
		expect(result.ok).toBe(true);
		if (result.ok) expect(result.profile.displayName).toBe('Updated');
		expect(mockFetch).toHaveBeenCalledWith('/api/profile', {
			method: 'PUT',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ displayName: 'Updated' })
		});
	});

	it('returns validation error on 400', async () => {
		const body = { errors: { DisplayName: ['Display name must be 200 characters or fewer.'] } };
		mockFetch.mockResolvedValue({
			ok: false,
			status: 400,
			text: () => Promise.resolve(JSON.stringify(body))
		});
		const result = await updateProfile({ displayName: 'x'.repeat(201) });
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error).toContain('200 characters');
	});

	it('returns generic error on other failures', async () => {
		mockFetch.mockResolvedValue({ ok: false, status: 500, text: () => Promise.resolve('Server error') });
		const result = await updateProfile({ displayName: 'Test' });
		expect(result.ok).toBe(false);
		if (!result.ok) expect(result.error).toContain('Server error');
	});
});

describe('login', () => {
	it('sets window.location.href correctly', () => {
		const loc = { href: '' };
		Object.defineProperty(window, 'location', { value: loc, writable: true, configurable: true });
		login('/dashboard');
		expect(loc.href).toBe('/auth/login?returnUrl=%2Fdashboard');
	});

	it('defaults returnUrl to /', () => {
		const loc = { href: '' };
		Object.defineProperty(window, 'location', { value: loc, writable: true, configurable: true });
		login();
		expect(loc.href).toBe('/auth/login?returnUrl=%2F');
	});
});
