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
	<table>
		<thead>
			<tr>
				<th>Game</th>
				<th>Date</th>
				<th>Players</th>
				<th>Duration</th>
				<th>Location</th>
			</tr>
		</thead>
		<tbody>
			{#each plays as play}
				<tr>
					<td><strong>{play.gameName}</strong></td>
					<td>{new Date(play.datePlayed).toLocaleDateString()}</td>
					<td>{play.playerCount}</td>
					<td>{play.duration ?? '-'}</td>
					<td>{play.locationName ?? '-'}</td>
				</tr>
			{/each}
		</tbody>
	</table>

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

	table {
		width: 100%;
		border-collapse: collapse;
		background: var(--color-surface);
		border-radius: var(--radius);
		overflow: hidden;
	}

	th,
	td {
		text-align: left;
		padding: 0.75rem 1rem;
		border-bottom: 1px solid var(--color-border);
	}

	th {
		background: var(--color-surface-alt);
		font-size: 0.85rem;
		text-transform: uppercase;
		color: var(--color-text-muted);
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
