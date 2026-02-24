export interface User {
	userId: string;
	username: string;
	avatarUrl: string | null;
}

export interface PlaySummary {
	id: string;
	gameName: string;
	datePlayed: string;
	duration: string | null;
	locationName: string | null;
	playerCount: number;
	hasWinner: boolean;
}

export interface PlaysResponse {
	plays: PlaySummary[];
	totalCount: number;
	page: number;
	pageSize: number;
}

export interface GroupSummary {
	id: string;
	name: string;
	allowSharing: boolean;
}

export interface UploadResult {
	uploadId: string;
	playCount: number;
}

export async function fetchUser(): Promise<User | null> {
	const res = await fetch('/auth/me');
	if (!res.ok) return null;
	return res.json();
}

export function login(returnUrl = '/'): void {
	window.location.href = `/auth/login?returnUrl=${encodeURIComponent(returnUrl)}`;
}

export async function logout(): Promise<void> {
	await fetch('/auth/logout', { method: 'POST' });
	window.location.href = '/';
}

export async function fetchPlays(page = 1, pageSize = 20): Promise<PlaysResponse> {
	const res = await fetch(`/api/plays?page=${page}&pageSize=${pageSize}`);
	if (!res.ok) return { plays: [], totalCount: 0, page, pageSize };
	return res.json();
}

export async function fetchGroups(): Promise<GroupSummary[]> {
	const res = await fetch('/api/groups');
	if (!res.ok) return [];
	return res.json();
}

export async function uploadPlayFile(file: File): Promise<{ ok: true; result: UploadResult } | { ok: false; error: string }> {
	const form = new FormData();
	form.append('file', file);
	const res = await fetch('/api/plays/upload', { method: 'POST', body: form });
	if (res.status === 401) return { ok: false, error: 'Not authenticated. Please sign in.' };
	if (!res.ok) {
		const text = await res.text();
		return { ok: false, error: text || `Upload failed (${res.status})` };
	}
	return { ok: true, result: await res.json() };
}
