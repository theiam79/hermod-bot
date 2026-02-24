<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { user, loading } from '$lib/stores/auth';
	import { fetchPlays, fetchGroups, type PlaySummary, type GroupSummary } from '$lib/api';

	let recentPlays = $state<PlaySummary[]>([]);
	let groups = $state<GroupSummary[]>([]);

	onMount(async () => {
		const unsub = user.subscribe(async (u) => {
			if (!$loading && !u) {
				goto('/');
				return;
			}
			if (u) {
				const [playsRes, groupsRes] = await Promise.all([fetchPlays(1, 5), fetchGroups()]);
				recentPlays = playsRes.plays;
				groups = groupsRes;
			}
		});
		return unsub;
	});
</script>

<h1>Dashboard</h1>

<section class="card" style="margin-bottom: 1.5rem;">
	<h2>Recent plays</h2>
	{#if recentPlays.length === 0}
		<p class="muted">No plays yet. <a href="/upload">Upload a .bgsplay file</a> to get started.</p>
	{:else}
		<ul class="play-list">
			{#each recentPlays as play}
				<li>
					<strong>{play.gameName}</strong>
					<span class="meta">{new Date(play.datePlayed).toLocaleDateString()} &middot; {play.playerCount} players</span>
				</li>
			{/each}
		</ul>
		<a href="/plays">View all plays</a>
	{/if}
</section>

<section class="card">
	<h2>Groups</h2>
	{#if groups.length === 0}
		<p class="muted">You are not a member of any groups yet.</p>
	{:else}
		<ul class="group-list">
			{#each groups as group}
				<li>
					<strong>{group.name}</strong>
					{#if group.allowSharing}
						<span class="badge">Sharing on</span>
					{/if}
				</li>
			{/each}
		</ul>
		<a href="/groups">View all groups</a>
	{/if}
</section>

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	h2 {
		font-size: 1.1rem;
		margin-bottom: 0.75rem;
	}

	.muted {
		color: var(--color-text-muted);
	}

	.play-list,
	.group-list {
		list-style: none;
		margin-bottom: 0.75rem;
	}

	.play-list li,
	.group-list li {
		padding: 0.5rem 0;
		border-bottom: 1px solid var(--color-border);
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.meta {
		font-size: 0.85rem;
		color: var(--color-text-muted);
	}

	.badge {
		font-size: 0.75rem;
		background: var(--color-success);
		color: white;
		padding: 0.15em 0.5em;
		border-radius: 4px;
	}
</style>
