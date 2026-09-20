<script setup lang="ts">
import { RouterLink, RouterView } from 'vue-router'
import { useApplicationStore } from './stores/application'

const application = useApplicationStore()
</script>

<template>
  <div class="min-h-screen bg-stone-50 text-slate-900">
    <header class="border-b border-stone-200 bg-white">
      <div class="mx-auto flex max-w-6xl items-center justify-between px-6 py-4">
        <RouterLink class="text-lg font-bold tracking-tight text-emerald-800" to="/">
          {{ application.name }}
        </RouterLink>
        <span class="rounded-full bg-emerald-50 px-3 py-1 text-sm font-medium text-emerald-800">
          Nền tảng: {{ application.foundationStatus }}
        </span>
      </div>
    </header>

    <main class="mx-auto max-w-6xl px-6 py-12">
      <RouterView />
    </main>
  </div>
</template>
