<script lang="ts">
	import { onMount } from 'svelte';
	import { page } from '$app/stores';
	import { user, checkAuth } from '$lib/stores/auth';
	import { joinGroup, login } from '$lib/api';

	let status = $state<'loading' | 'joined' | 'already_member' | 'not_found' | 'unauthorized' | 'error' | 'missing_group'>('loading');

	onMount(async () => {
		const groupId = $page.url.searchParams.get('groupId');
		if (!groupId) {
			status = 'missing_group';
			return;
		}

		const u = await checkAuth();
		if (!u) {
			login(`/enroll?groupId=${groupId}`);
			return;
		}

		const result = await joinGroup(groupId);
		status = result.status;
	});
</script>

<div class="enroll-container">
	{#if status === 'loading'}
		<div class="card">
			<h1>Enrolling...</h1>
			<p>Please wait while we add you to the group.</p>
		</div>
	{:else if status === 'joined'}
		<div class="card success">
			<h1>You're enrolled!</h1>
			<p>Head back to Discord and start sharing plays.</p>
		</div>
	{:else if status === 'already_member'}
		<div class="card">
			<h1>Already enrolled</h1>
			<p>You're already a member of this group. Head back to Discord!</p>
		</div>
	{:else if status === 'not_found'}
		<div class="card error">
			<h1>Group not found</h1>
			<p>This group doesn't exist or the link may be invalid.</p>
		</div>
	{:else if status === 'unauthorized'}
		<div class="card error">
			<h1>Not authenticated</h1>
			<p>Redirecting you to sign in...</p>
		</div>
	{:else if status === 'missing_group'}
		<div class="card error">
			<h1>Missing group</h1>
			<p>No group ID was provided. Use the <code>/enroll</code> command in Discord to get a valid link.</p>
		</div>
	{:else}
		<div class="card error">
			<h1>Something went wrong</h1>
			<p>Please try again or use the <code>/enroll</code> command in Discord.</p>
		</div>
	{/if}
</div>

<style>
	.enroll-container {
		display: flex;
		justify-content: center;
		padding-top: 3rem;
	}

	.card {
		max-width: 28rem;
		text-align: center;
	}

	h1 {
		margin-bottom: 0.75rem;
	}

	.success h1 {
		color: var(--color-success);
	}

	.error h1 {
		color: var(--color-error);
	}

	code {
		background: var(--color-surface-alt);
		padding: 0.1em 0.4em;
		border-radius: 3px;
		font-size: 0.9em;
	}
</style>
