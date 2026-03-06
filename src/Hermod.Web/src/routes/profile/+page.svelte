<script lang="ts">
	import { onMount } from 'svelte';
	import { user, loading } from '$lib/stores/auth';
	import { fetchProfile, updateProfile, login, type ProfileResponse, type UpdateProfileRequest } from '$lib/api';

	let profile = $state<ProfileResponse | null>(null);
	let fetching = $state(true);

	let displayName = $state('');
	let bggUsername = $state('');
	let subscribeToPlays = $state(false);
	let postingEnabled = $state(true);
	let saving = $state(false);
	let successMessage = $state('');
	let errorMessage = $state('');

	onMount(() => {
		const unsub = user.subscribe((u) => {
			if (!$loading && !u) return;
			if (u) {
				fetchProfile().then((p) => {
					profile = p;
					if (p) {
						displayName = p.displayName;
						bggUsername = p.bggUsername ?? '';
						subscribeToPlays = p.subscribeToPlays;
					postingEnabled = p.postingEnabled;
					}
					fetching = false;
				});
			}
		});
		return unsub;
	});

	async function handleSave() {
		successMessage = '';
		errorMessage = '';

		if (displayName.length > 200) {
			errorMessage = 'Display name must be 200 characters or fewer.';
			return;
		}

		saving = true;
		const data: UpdateProfileRequest = {};
		if (displayName !== profile?.displayName) data.displayName = displayName;
		if (bggUsername !== (profile?.bggUsername ?? '')) data.bggUsername = bggUsername;
		if (subscribeToPlays !== profile?.subscribeToPlays) data.subscribeToPlays = subscribeToPlays;
		if (postingEnabled !== profile?.postingEnabled) data.postingEnabled = postingEnabled;

		if (Object.keys(data).length === 0) {
			successMessage = 'No changes to save.';
			saving = false;
			return;
		}

		const result = await updateProfile(data);
		saving = false;

		if (result.ok) {
			profile = result.profile;
			subscribeToPlays = result.profile.subscribeToPlays;
		postingEnabled = result.profile.postingEnabled;
			successMessage = 'Profile saved.';
		} else {
			errorMessage = result.error;
		}
	}
</script>

<h1>Profile</h1>

{#if $loading || fetching}
	<p>Loading...</p>
{:else if !$user}
	<div class="card">
		<p>Sign in to view your profile.</p>
		<button class="btn-primary" onclick={() => login('/profile')}>Sign in with Discord</button>
	</div>
{:else if !profile}
	<div class="card">
		<p>No profile found. Join a group to create your profile.</p>
	</div>
{:else}
	{#if successMessage}
		<div class="alert alert-success">{successMessage}</div>
	{/if}
	{#if errorMessage}
		<div class="alert alert-error">{errorMessage}</div>
	{/if}

	<form class="card" onsubmit={(e) => { e.preventDefault(); handleSave(); }}>
		<div class="field">
			<label for="displayName">Display name</label>
			<input
				id="displayName"
				type="text"
				bind:value={displayName}
				maxlength="200"
			/>
			<span class="hint">{displayName.length}/200</span>
		</div>

		<div class="field">
			<label for="bggUsername">BGG username</label>
			<input
				id="bggUsername"
				type="text"
				bind:value={bggUsername}
				maxlength="200"
			/>
		</div>

		{#if profile.bggId != null}
			<div class="field">
				<label>BGG ID</label>
				<span class="readonly-value">{profile.bggId}</span>
			</div>
		{/if}

		<div class="field checkbox-field">
			<input
				id="postingEnabled"
				type="checkbox"
				bind:checked={postingEnabled}
			/>
			<label for="postingEnabled">Post plays to groups</label>
		</div>

		<div class="field checkbox-field">
			<input
				id="subscribeToPlays"
				type="checkbox"
				bind:checked={subscribeToPlays}
			/>
			<label for="subscribeToPlays">Subscribe to play notifications</label>
		</div>

		<button class="btn-primary" type="submit" disabled={saving}>
			{saving ? 'Saving...' : 'Save'}
		</button>
	</form>

	<section class="card groups-section">
		<h2>Groups</h2>
		{#if profile.groups.length === 0}
			<p class="muted">You are not a member of any groups.</p>
		{:else}
			<ul class="group-list">
				{#each profile.groups as group}
					<li>{group.name}</li>
				{/each}
			</ul>
		{/if}
	</section>
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	form {
		margin-bottom: 1.5rem;
	}

	.field {
		margin-bottom: 1rem;
	}

	label {
		display: block;
		font-size: 0.9rem;
		margin-bottom: 0.25rem;
		color: var(--color-text-muted);
	}

	input {
		width: 100%;
		padding: 0.5rem 0.75rem;
		border-radius: var(--radius);
		border: 1px solid var(--color-border);
		background: var(--color-bg);
		color: var(--color-text);
		font-size: 1rem;
		font-family: inherit;
	}

	input:focus {
		outline: none;
		border-color: var(--color-primary);
	}

	.readonly-value {
		color: var(--color-text-muted);
	}

	.checkbox-field {
		display: flex;
		align-items: center;
		gap: 0.5rem;
	}

	.checkbox-field input[type='checkbox'] {
		width: auto;
	}

	.checkbox-field label {
		display: inline;
		margin-bottom: 0;
	}

	.hint {
		display: block;
		font-size: 0.75rem;
		color: var(--color-text-muted);
		margin-top: 0.25rem;
		text-align: right;
	}

	.groups-section {
		margin-bottom: 1.5rem;
	}

	h2 {
		font-size: 1.1rem;
		margin-bottom: 0.75rem;
	}

	.muted {
		color: var(--color-text-muted);
	}

	.group-list {
		list-style: none;
	}

	.group-list li {
		padding: 0.5rem 0;
		border-bottom: 1px solid var(--color-border);
	}

	.group-list li:last-child {
		border-bottom: none;
	}
</style>
