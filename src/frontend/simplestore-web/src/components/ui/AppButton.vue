<script setup lang="ts">
withDefaults(defineProps<{
  variant?: 'primary' | 'secondary' | 'danger' | 'text'
  type?: 'button' | 'submit' | 'reset'
  disabled?: boolean
}>(), {
  variant: 'secondary',
  type: 'button',
  disabled: false,
})
</script>

<template>
  <button
    class="app-button"
    :class="`app-button--${variant}`"
    :type="type"
    :disabled="disabled"
  >
    <slot />
  </button>
</template>

<style scoped>
.app-button {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 0.5rem;
  min-height: 2.5rem;
  padding: 0.5rem 0.875rem;
  border: 1px solid transparent;
  border-radius: var(--radius-md, 0.625rem);
  font: inherit;
  font-weight: 600;
  line-height: 1.25;
  text-align: center;
  cursor: pointer;
}

.app-button:focus-visible {
  outline: 3px solid var(--focus-ring, #2563eb);
  outline-offset: 2px;
}

.app-button:disabled {
  cursor: not-allowed;
  opacity: 0.55;
}

.app-button--primary {
  background: var(--brand-primary, #047857);
  color: var(--text-on-brand, #fff);
}

.app-button--primary:hover:not(:disabled) {
  background: var(--brand-primary-hover, #065f46);
}

.app-button--secondary {
  border-color: var(--border, #d6d3d1);
  background: var(--surface, #fff);
  color: var(--text, #0f172a);
}

.app-button--secondary:hover:not(:disabled) {
  background: var(--surface-muted, #f5f5f4);
}

.app-button--danger {
  background: var(--danger, #b91c1c);
  color: #fff;
}

.app-button--danger:hover:not(:disabled) {
  filter: brightness(0.9);
}

.app-button--text {
  background: transparent;
  color: var(--brand-primary, #047857);
}

.app-button--text:hover:not(:disabled) {
  background: var(--surface-muted, #f5f5f4);
}
</style>
