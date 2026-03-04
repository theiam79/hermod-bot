<script lang="ts">
	import { onMount } from 'svelte';
	import { user, loading } from '$lib/stores/auth';
	import { fetchPlays, login, type PlaySummary } from '$lib/api';

	let plays = $state<PlaySummary[]>([]);
	let totalCount = $state(0);
	let currentPage = $state(1);
	let pageSize = 20;
	let fetching = $state(true);

	async function loadPage(p: number) {
		fetching = true;
		const res = await fetchPlays(p, pageSize);
		plays = res.plays;
		totalCount = res.totalCount;
		currentPage = res.page;
		fetching = false;
	}

	onMount(() => {
		const unsub = user.subscribe((u) => {
			if (!$loading && !u) return;
			if (u) loadPage(1);
		});
		return unsub;
	});

	const totalPages = $derived(Math.max(1, Math.ceil(totalCount / pageSize)));
</script>

<h1>Plays</h1>

{#if $loading || fetching}
	<p>Loading...</p>
{:else if !$user}
	<div class="card">
		<p>Sign in to view your plays.</p>
		<button class="btn-primary" onclick={() => login('/plays')}>Sign in with Discord</button>
	</div>
{:else if plays.length === 0}
	<div class="card">
		<p>No plays found. <a href="/upload">Upload a .bgsplay file</a> to get started.</p>
	</div>
{:else}
	<div class="play-list">
		{#each plays as play}
			<div class="card play-card">
				<div class="play-header">
					<strong>{play.gameName}</strong>
					<span class="meta">{new Date(play.datePlayed).toLocaleDateString()}</span>
				</div>
				<div class="play-details">
					<span>{play.playerCount} player{play.playerCount === 1 ? '' : 's'}</span>
					{#if play.duration}<span>{play.duration} min</span>{/if}
					{#if play.locationName}<span>{play.locationName}</span>{/if}
				</div>
			</div>
		{/each}
	</div>

	{#if totalPages > 1}
		<div class="pagination">
			<button class="btn-secondary" disabled={currentPage <= 1} onclick={() => loadPage(currentPage - 1)}>Previous</button>
			<span>Page {currentPage} of {totalPages}</span>
			<button class="btn-secondary" disabled={currentPage >= totalPages} onclick={() => loadPage(currentPage + 1)}>Next</button>
		</div>
	{/if}
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	.play-list {
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.play-card {
		padding: 1rem;
	}

	.play-header {
		display: flex;
		justify-content: space-between;
		align-items: baseline;
		gap: 0.5rem;
		margin-bottom: 0.35rem;
	}

	.play-details {
		display: flex;
		flex-wrap: wrap;
		gap: 0.5rem;
		font-size: 0.85rem;
		color: var(--color-text-muted);
	}

	.play-details span:not(:last-child)::after {
		content: '\00b7';
		margin-left: 0.5rem;
	}

	.meta {
		font-size: 0.85rem;
		color: var(--color-text-muted);
		white-space: nowrap;
	}

	.pagination {
		display: flex;
		align-items: center;
		justify-content: center;
		gap: 1rem;
		margin-top: 1.5rem;
	}

	.pagination span {
		color: var(--color-text-muted);
		font-size: 0.9rem;
	}
</style>
