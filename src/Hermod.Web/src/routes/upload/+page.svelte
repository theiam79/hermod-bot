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
	let dragOver = $state(false);

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

	function onDrop(e: DragEvent) {
		e.preventDefault();
		dragOver = false;
		const file = e.dataTransfer?.files[0];
		if (file) handleUpload(file);
	}

	function onDragOver(e: DragEvent) {
		e.preventDefault();
		dragOver = true;
	}

	function onDragLeave() {
		dragOver = false;
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
	<div class="card loading-card">
		<div class="spinner"></div>
		<p>Loading...</p>
	</div>
{:else if !$user}
	<div class="card sign-in-card">
		<svg class="icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
			<rect x="3" y="11" width="18" height="11" rx="2" ry="2"></rect>
			<path d="M7 11V7a5 5 0 0 1 10 0v4"></path>
		</svg>
		<p>Sign in to upload your BGStats plays.</p>
		<button class="btn-primary" onclick={() => login('/upload')}>Sign in with Discord</button>
	</div>
{:else}
	{#if result}
		<div class="alert alert-success">
			<svg class="alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
				<path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path>
				<polyline points="22 4 12 14.01 9 11.01"></polyline>
			</svg>
			<span>Uploaded <strong>{result.playCount}</strong> play{result.playCount === 1 ? '' : 's'} successfully.</span>
		</div>
	{/if}

	{#if error}
		<div class="alert alert-error">
			<svg class="alert-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round">
				<circle cx="12" cy="12" r="10"></circle>
				<line x1="15" y1="9" x2="9" y2="15"></line>
				<line x1="9" y1="9" x2="15" y2="15"></line>
			</svg>
			<span>{error}</span>
		</div>
	{/if}

	<!-- svelte-ignore a11y_no_static_element_interactions -->
	<label
		class="drop-zone"
		class:drag-over={dragOver}
		class:uploading
		ondrop={onDrop}
		ondragover={onDragOver}
		ondragleave={onDragLeave}
	>
		<input
			type="file"
			accept=".bgsplay"
			onchange={onFileSelect}
			bind:this={fileInput}
			disabled={uploading}
		/>

		{#if uploading}
			<div class="spinner"></div>
			<p class="drop-text">Uploading...</p>
		{:else}
			<svg class="upload-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.5" stroke-linecap="round" stroke-linejoin="round">
				<path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"></path>
				<polyline points="17 8 12 3 7 8"></polyline>
				<line x1="12" y1="3" x2="12" y2="15"></line>
			</svg>
			<p class="drop-text">
				{#if dragOver}
					Drop file here
				{:else}
					Choose a <code>.bgsplay</code> file
				{/if}
			</p>
			<p class="drop-subtext">or drag and drop</p>
		{/if}
	</label>

	<p class="hint">
		{#if /iPad|iPhone|iPod/.test(navigator.userAgent)}
			In BGStats, export your plays as a .bgsplay file to the Files app, then select it here.
		{:else}
			On Android, you can share .bgsplay files directly from BGStats to Hermod.
		{/if}
	</p>
{/if}

<style>
	h1 {
		margin-bottom: 1.5rem;
	}

	/* Sign-in card */
	.sign-in-card {
		text-align: center;
		padding: 3rem 1.5rem;
	}

	.sign-in-card .icon {
		width: 48px;
		height: 48px;
		color: var(--color-text-muted);
		margin-bottom: 1rem;
	}

	.sign-in-card p {
		color: var(--color-text-muted);
		margin-bottom: 1.5rem;
	}

	/* Loading card */
	.loading-card {
		text-align: center;
		padding: 3rem 1.5rem;
	}

	.loading-card p {
		color: var(--color-text-muted);
		margin-top: 1rem;
	}

	/* Drop zone */
	.drop-zone {
		display: flex;
		flex-direction: column;
		align-items: center;
		justify-content: center;
		padding: 2.5rem 1.5rem;
		border: 2px dashed var(--color-border);
		border-radius: var(--radius);
		background: var(--color-surface);
		cursor: pointer;
		transition: border-color 0.2s, background 0.2s;
		text-align: center;
	}

	.drop-zone:hover {
		border-color: var(--color-primary);
		background: color-mix(in srgb, var(--color-primary) 5%, var(--color-surface));
	}

	.drop-zone.drag-over {
		border-color: var(--color-primary);
		background: color-mix(in srgb, var(--color-primary) 10%, var(--color-surface));
		border-style: solid;
	}

	.drop-zone.uploading {
		cursor: default;
		border-style: solid;
		border-color: var(--color-primary);
	}

	.drop-zone input {
		position: absolute;
		width: 1px;
		height: 1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
	}

	.upload-icon {
		width: 40px;
		height: 40px;
		color: var(--color-text-muted);
		margin-bottom: 0.75rem;
		transition: color 0.2s;
	}

	.drop-zone:hover .upload-icon {
		color: var(--color-primary);
	}

	.drop-zone.drag-over .upload-icon {
		color: var(--color-primary);
	}

	.drop-text {
		font-size: 1rem;
		font-weight: 500;
		color: var(--color-text);
		margin: 0;
	}

	.drop-text code {
		background: var(--color-surface-alt);
		padding: 0.1em 0.4em;
		border-radius: 4px;
		font-size: 0.9em;
	}

	.drop-subtext {
		font-size: 0.85rem;
		color: var(--color-text-muted);
		margin-top: 0.25rem;
	}

	.hint {
		color: var(--color-text-muted);
		font-size: 0.85em;
		margin-top: 1rem;
		text-align: center;
	}

	@media (min-width: 640px) and (hover: hover) {
		.hint {
			display: none;
		}
	}

	/* Alerts */
	.alert {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		margin-bottom: 1rem;
	}

	.alert-icon {
		width: 20px;
		height: 20px;
		flex-shrink: 0;
	}

	.alert-success .alert-icon {
		color: var(--color-success);
	}

	.alert-error .alert-icon {
		color: var(--color-error);
	}

	/* Spinner */
	.spinner {
		width: 32px;
		height: 32px;
		border: 3px solid var(--color-border);
		border-top-color: var(--color-primary);
		border-radius: 50%;
		animation: spin 0.8s linear infinite;
		margin-bottom: 0.5rem;
	}

	@keyframes spin {
		to { transform: rotate(360deg); }
	}

	/* Desktop */
	@media (min-width: 640px) {
		.drop-zone {
			padding: 3.5rem 2rem;
		}

		.upload-icon {
			width: 48px;
			height: 48px;
		}

		.drop-text {
			font-size: 1.1rem;
		}
	}
</style>
