<script setup lang="ts">
const foundations = [
  'Vue 3 + TypeScript',
  'Vue Router + Pinia',
  'Tailwind CSS',
  'Vitest + Vue Test Utils',
]
</script>

<template>
  <section class="overflow-hidden rounded-3xl border border-stone-200 bg-white shadow-sm">
    <div class="grid gap-10 p-8 md:grid-cols-[1.35fr_1fr] md:p-12">
      <div>
        <p class="mb-3 text-sm font-semibold uppercase tracking-[0.18em] text-emerald-700">
          Engineering Foundation
        </p>
        <h1 class="max-w-2xl text-4xl font-bold tracking-tight text-slate-950 md:text-5xl">
          SimpleStore đã sẵn sàng cho vertical slice đầu tiên.
        </h1>
        <p class="mt-5 max-w-xl text-lg leading-8 text-slate-600">
          Shell tối thiểu này xác nhận frontend chạy độc lập. Chức năng Product và Inventory sẽ
          chỉ được bổ sung trong Slice 1.
        </p>
      </div>

      <ul class="grid content-center gap-3" aria-label="Nền tảng frontend">
        <li
          v-for="foundation in foundations"
          :key="foundation"
          class="flex items-center gap-3 rounded-2xl bg-stone-50 px-4 py-3 text-sm font-medium text-slate-700"
        >
          <span class="h-2.5 w-2.5 rounded-full bg-emerald-500" aria-hidden="true" />
          {{ foundation }}
        </li>
      </ul>
    </div>
  </section>
</template>
