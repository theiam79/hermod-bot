<script lang="ts">
	import { onMount } from 'svelte';

	let deferredPrompt: BeforeInstallPromptEvent | null = $state(null);
	let dismissed = $state(false);
	let installed = $state(false);

	onMount(() => {
		// Check if already installed (display-mode: standalone)
		if (window.matchMedia('(display-mode: standalone)').matches) {
			installed = true;
			return;
		}

		// Check for event captured early in app.html before Svelte mounted
		if (window.deferredInstallPrompt) {
			deferredPrompt = window.deferredInstallPrompt;
		}

		const handler = (e: Event) => {
			e.preventDefault();
			deferredPrompt = e as BeforeInstallPromptEvent;
		};

		window.addEventListener('beforeinstallprompt', handler);
		window.addEventListener('appinstalled', () => {
			installed = true;
			deferredPrompt = null;
		});

		return () => window.removeEventListener('beforeinstallprompt', handler);
	});

	async function install() {
		if (!deferredPrompt) return;
		deferredPrompt.prompt();
		const { outcome } = await deferredPrompt.userChoice;
		if (outcome === 'accepted') {
			installed = true;
		}
		deferredPrompt = null;
	}
</script>

{#if deferredPrompt && !dismissed && !installed}
	<div class="install-banner">
		<p>Install Hermod to share .bgsplay files directly from BGStats</p>
		<div class="install-actions">
			<button class="btn-primary" onclick={install}>Install</button>
			<button class="btn-secondary" onclick={() => (dismissed = true)}>Not now</button>
		</div>
	</div>
{/if}

<style>
	.install-banner {
		background: var(--color-surface);
		border: 1px solid var(--color-primary);
		border-radius: var(--radius);
		padding: 1rem 1.25rem;
		margin-bottom: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.75rem;
	}

	.install-banner p {
		font-size: 0.95rem;
	}

	.install-actions {
		display: flex;
		gap: 0.5rem;
	}

	.install-actions button {
		font-size: 0.85rem;
		padding: 0.4em 1em;
	}
</style>
