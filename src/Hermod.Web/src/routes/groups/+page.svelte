<script lang="ts">
	import { onMount } from 'svelte';
	import { user, loading } from '$lib/stores/auth';
	import { fetchGroups, leaveGroup, login, type GroupSummary } from '$lib/api';

	let groups = $state<GroupSummary[]>([]);
	let fetching = $state(true);
	let error = $state<string | null>(null);
	let leaving = $state<string | null>(null);

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

	async function handleLeave(group: GroupSummary) {
		if (!confirm(`Are you sure you want to leave **${group.name}**?`)) return;
		error = null;
		leaving = group.id;
		const result = await leaveGroup(group.id);
		leaving = null;
		if (result.status === 'left') {
			groups = groups.filter((g) => g.id !== group.id);
		} else if (result.status === 'not_found') {
			error = 'Group not found.';
		} else if (result.status === 'unauthorized') {
			error = 'You must be signed in to leave a group.';
		} else {
			error = 'Something went wrong. Please try again.';
		}
	}
</script>

<h1>Groups</h1>

{#if error}
	<div class="error">{error}</div>
{/if}

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
				<div class="group-info">
					<h2>{group.name}</h2>
					<span class="status" class:on={group.allowSharing}>
						Sharing {group.allowSharing ? 'enabled' : 'disabled'}
					</span>
				</div>
				<button
					class="btn-leave"
					disabled={leaving === group.id}
					onclick={() => handleLeave(group)}
				>
					{leaving === group.id ? 'Leaving...' : 'Leave'}
				</button>
			</div>
		{/each}
	</div>
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	.error {
		background: color-mix(in srgb, var(--color-error, #e53e3e) 15%, transparent);
		color: var(--color-error, #e53e3e);
		padding: 0.75rem 1rem;
		border-radius: 6px;
		margin-bottom: 1rem;
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

	.group-info {
		display: flex;
		flex-wrap: wrap;
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

	.btn-leave {
		padding: 0.4em 0.8em;
		font-size: 0.85rem;
		border: 1px solid var(--color-error, #e53e3e);
		border-radius: 4px;
		background: transparent;
		color: var(--color-error, #e53e3e);
		cursor: pointer;
		transition: background 0.15s, color 0.15s;
	}

	.btn-leave:hover:not(:disabled) {
		background: var(--color-error, #e53e3e);
		color: white;
	}

	.btn-leave:disabled {
		opacity: 0.5;
		cursor: not-allowed;
	}
</style>
