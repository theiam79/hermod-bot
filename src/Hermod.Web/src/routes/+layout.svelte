<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { user, loading, checkAuth } from '$lib/stores/auth';
	import { login, logout } from '$lib/api';
	import '../app.css';

	let { children } = $props();

	onMount(() => {
		checkAuth();
	});
</script>

<nav>
	<div class="nav-inner">
		<a href="/" class="logo">Hermod</a>
		{#if $user}
			<div class="nav-links">
				<a href="/dashboard" class:active={$page.url.pathname === '/dashboard'}>Dashboard</a>
				<a href="/upload" class:active={$page.url.pathname === '/upload'}>Upload</a>
				<a href="/plays" class:active={$page.url.pathname === '/plays'}>Plays</a>
				<a href="/groups" class:active={$page.url.pathname === '/groups'}>Groups</a>
			</div>
			<div class="nav-user">
				<span>{$user.username}</span>
				<button class="btn-secondary" onclick={() => logout()}>Sign out</button>
			</div>
		{:else if !$loading}
			<button class="btn-primary" onclick={() => login()}>Sign in with Discord</button>
		{/if}
	</div>
</nav>

<main class="container">
	{@render children()}
</main>

<style>
	nav {
		background: var(--color-surface);
		border-bottom: 1px solid var(--color-border);
		padding: 0.75rem 1rem;
	}

	.nav-inner {
		max-width: var(--max-width);
		margin: 0 auto;
		display: flex;
		align-items: center;
		gap: 1.5rem;
	}

	.logo {
		font-size: 1.25rem;
		font-weight: 700;
		color: var(--color-primary);
	}

	.logo:hover {
		text-decoration: none;
	}

	.nav-links {
		display: flex;
		gap: 1rem;
		flex: 1;
	}

	.nav-links a {
		color: var(--color-text-muted);
		padding: 0.25rem 0;
	}

	.nav-links a.active,
	.nav-links a:hover {
		color: var(--color-text);
	}

	.nav-user {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.nav-user span {
		color: var(--color-text-muted);
		font-size: 0.9rem;
	}

	main {
		padding-top: 2rem;
		padding-bottom: 2rem;
	}
</style>
