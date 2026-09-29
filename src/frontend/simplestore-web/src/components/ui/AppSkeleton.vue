<template>
  <span class="app-skeleton" role="status" aria-label="Đang tải" />
</template>

<style scoped>
.app-skeleton {
  display: block;
  width: 100%;
  min-height: 1rem;
  border-radius: var(--radius-sm, 0.375rem);
  background: var(--surface-muted, #e7e5e4);
}
</style>
