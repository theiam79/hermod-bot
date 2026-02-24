import { writable } from 'svelte/store';
import { fetchUser, type User } from '$lib/api';

export const user = writable<User | null>(null);
export const loading = writable(true);

export async function checkAuth(): Promise<User | null> {
	loading.set(true);
	const u = await fetchUser();
	user.set(u);
	loading.set(false);
	return u;
}
