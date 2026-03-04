<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { user, loading, checkAuth } from '$lib/stores/auth';
	import { login, logout } from '$lib/api';
	import '../app.css';

	let { children } = $props();
	let menuOpen = $state(false);

	// Close menu on navigation
	$effect(() => {
		$page.url.pathname;
		menuOpen = false;
	});

	onMount(() => {
		checkAuth();
	});
</script>

<nav>
	<div class="nav-inner">
		<a href="/" class="logo">Hermod</a>
		{#if $user}
			<button
				class="menu-toggle"
				aria-label="Menu"
				aria-expanded={menuOpen}
				onclick={() => (menuOpen = !menuOpen)}
			>
				{menuOpen ? '✕' : '☰'}
			</button>
			<div class="nav-links" class:open={menuOpen}>
				<a href="/dashboard" class:active={$page.url.pathname === '/dashboard'}>Dashboard</a>
				<a href="/upload" class:active={$page.url.pathname === '/upload'}>Upload</a>
				<a href="/plays" class:active={$page.url.pathname === '/plays'}>Plays</a>
				<a href="/groups" class:active={$page.url.pathname === '/groups'}>Groups</a>
				<button class="btn-secondary sign-out" onclick={() => logout()}>Sign out</button>
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
		padding: 0.5rem 1rem;
	}

	.nav-inner {
		max-width: var(--max-width);
		margin: 0 auto;
		display: flex;
		flex-wrap: wrap;
		align-items: center;
		gap: 0.5rem;
	}

	.logo {
		font-size: 1.25rem;
		font-weight: 700;
		color: var(--color-primary);
		margin-right: auto;
	}

	.logo:hover {
		text-decoration: none;
	}

	.menu-toggle {
		background: transparent;
		border: none;
		color: var(--color-text);
		font-size: 1.5rem;
		padding: 0.25rem 0.5rem;
		cursor: pointer;
		line-height: 1;
	}

	.nav-links {
		display: none;
		flex-direction: column;
		gap: 0.25rem;
		order: 1;
		width: 100%;
	}

	.nav-links.open {
		display: flex;
	}

	.nav-links a {
		color: var(--color-text-muted);
		padding: 0.6rem 0.75rem;
		border-radius: var(--radius);
		font-size: 0.9rem;
	}

	.nav-links a.active {
		color: var(--color-text);
		background: var(--color-surface-alt);
	}

	.nav-links a:hover {
		color: var(--color-text);
		text-decoration: none;
	}

	.sign-out {
		font-size: 0.85rem;
		padding: 0.4em 0.8em;
		margin-top: 0.25rem;
		align-self: flex-start;
	}

	main {
		padding-top: 1.5rem;
		padding-bottom: 2rem;
	}

	@media (min-width: 640px) {
		nav {
			padding: 0.75rem 1rem;
		}

		.nav-inner {
			gap: 1.5rem;
		}

		.menu-toggle {
			display: none;
		}

		.nav-links {
			display: flex;
			flex-direction: row;
			width: auto;
			order: 0;
			flex: 1;
			gap: 0.5rem;
		}

		.nav-links a {
			padding: 0.4rem 0.6rem;
			font-size: 1rem;
		}

		.sign-out {
			margin-top: 0;
			align-self: center;
			margin-left: auto;
		}

		main {
			padding-top: 2rem;
		}
	}
</style>
