<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { user, loading } from '$lib/stores/auth';
	import { login, uploadPlayFile, type UploadResult } from '$lib/api';

	let fileInput = $state<HTMLInputElement | null>(null);
	let uploading = $state(false);
	let result = $state<UploadResult | null>(null);
	let error = $state<string | null>(null);

	async function handleUpload(file: File) {
		uploading = true;
		error = null;
		result = null;

		const res = await uploadPlayFile(file);
		uploading = false;

		if (res.ok) {
			result = res.result;
		} else {
			if (res.error.includes('Not authenticated')) {
				login('/upload');
				return;
			}
			error = res.error;
		}
	}

	function onFileSelect(e: Event) {
		const input = e.target as HTMLInputElement;
		const file = input.files?.[0];
		if (file) handleUpload(file);
	}

	onMount(async () => {
		// Check if arriving from share target
		const shared = $page.url.searchParams.get('shared');
		if (shared === 'true') {
			try {
				const cache = await caches.open('share-target');
				const cachedResponse = await cache.match('/shared-file');
				if (cachedResponse) {
					const blob = await cachedResponse.blob();
					const file = new File([blob], 'shared.bgsplay', { type: 'application/octet-stream' });
					await cache.delete('/shared-file');
					handleUpload(file);
				}
			} catch {
				// Share target cache miss — user can upload manually
			}
		}
	});
</script>

<h1>Upload plays</h1>

{#if $loading}
	<p>Loading...</p>
{:else if !$user}
	<div class="card">
		<p>You need to sign in to upload plays.</p>
		<button class="btn-primary" onclick={() => login('/upload')}>Sign in with Discord</button>
	</div>
{:else}
	<div class="card">
		<p>Select a <code>.bgsplay</code> file from BGStats to upload your plays.</p>

		<label class="file-label">
			<input
				type="file"
				accept=".bgsplay"
				onchange={onFileSelect}
				bind:this={fileInput}
				disabled={uploading}
			/>
			<span class="btn-primary">{uploading ? 'Uploading...' : 'Choose file'}</span>
		</label>

		<p class="hint">
			{#if /iPad|iPhone|iPod/.test(navigator.userAgent)}
				In BGStats, export your plays as a .bgsplay file to the Files app, then select it here.
			{:else}
				On Android, you can share .bgsplay files directly from BGStats to Hermod.
			{/if}
		</p>

		{#if result}
			<div class="alert alert-success">
				Uploaded <strong>{result.playCount}</strong> play{result.playCount === 1 ? '' : 's'} successfully.
			</div>
		{/if}

		{#if error}
			<div class="alert alert-error">{error}</div>
		{/if}
	</div>
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	p {
		margin-bottom: 1rem;
	}

	code {
		background: var(--color-surface-alt);
		padding: 0.1em 0.4em;
		border-radius: 4px;
		font-size: 0.9em;
	}

	.file-label {
		display: inline-block;
		margin-bottom: 1.5rem;
		cursor: pointer;
	}

	.file-label input {
		position: absolute;
		width: 1px;
		height: 1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
	}

	.hint {
		color: var(--color-text-muted);
		font-size: 0.85em;
	}
</style>
