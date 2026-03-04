<script lang="ts">
	import { onMount } from 'svelte';
	import { user, loading } from '$lib/stores/auth';
	import { fetchGroups, login, type GroupSummary } from '$lib/api';

	let groups = $state<GroupSummary[]>([]);
	let fetching = $state(true);

	onMount(() => {
		const unsub = user.subscribe((u) => {
			if (!$loading && !u) return;
			if (u) {
				fetchGroups().then((g) => {
					groups = g;
					fetching = false;
				});
			}
		});
		return unsub;
	});
</script>

<h1>Groups</h1>

{#if $loading || fetching}
	<p>Loading...</p>
{:else if !$user}
	<div class="card">
		<p>Sign in to view your groups.</p>
		<button class="btn-primary" onclick={() => login('/groups')}>Sign in with Discord</button>
	</div>
{:else if groups.length === 0}
	<div class="card">
		<p>You are not a member of any groups yet.</p>
	</div>
{:else}
	<div class="group-grid">
		{#each groups as group}
			<div class="card group-card">
				<h2>{group.name}</h2>
				<span class="status" class:on={group.allowSharing}>
					Sharing {group.allowSharing ? 'enabled' : 'disabled'}
				</span>
			</div>
		{/each}
	</div>
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	.group-grid {
		display: grid;
		gap: 1rem;
	}

	.group-card {
		display: flex;
		flex-wrap: wrap;
		justify-content: space-between;
		align-items: center;
		gap: 0.5rem;
	}

	h2 {
		font-size: 1.1rem;
	}

	.status {
		font-size: 0.8rem;
		padding: 0.2em 0.6em;
		border-radius: 4px;
		background: var(--color-surface-alt);
		color: var(--color-text-muted);
	}

	.status.on {
		background: color-mix(in srgb, var(--color-success) 20%, transparent);
		color: var(--color-success);
	}
</style>
