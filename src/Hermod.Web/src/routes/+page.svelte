<script lang="ts">
	import { user, loading } from '$lib/stores/auth';
	import { login } from '$lib/api';
</script>

<div class="hero">
	<h1>Hermod</h1>
	<p>Share your board game plays from BGStats with your groups.</p>

	{#if $loading}
		<p class="muted">Loading...</p>
	{:else if $user}
		<p>Welcome back, <strong>{$user.username}</strong>!</p>
		<div class="actions">
			<a href="/upload" class="btn-primary" role="button">Upload plays</a>
			<a href="/dashboard" class="btn-secondary" role="button">Dashboard</a>
		</div>
	{:else}
		<button class="btn-primary" onclick={() => login()}>Sign in with Discord</button>
	{/if}
</div>

<style>
	.hero {
		text-align: center;
		padding: 4rem 1rem;
	}

	h1 {
		font-size: 3rem;
		color: var(--color-primary);
		margin-bottom: 0.5rem;
	}

	p {
		color: var(--color-text-muted);
		margin-bottom: 1.5rem;
		font-size: 1.1rem;
	}

	.muted {
		color: var(--color-text-muted);
	}

	.actions {
		display: flex;
		gap: 1rem;
		justify-content: center;
	}

	[role='button'] {
		display: inline-block;
		padding: 0.6em 1.2em;
		border-radius: var(--radius);
		font-size: 1rem;
	}
</style>
