<template>
  <div class="h-full min-h-[24rem] rounded-2xl border border-brand-border bg-brand-card/60 p-6 shadow-xl shadow-black/20 backdrop-blur-xl">
    <div class="mb-5">
      <h2 class="text-lg font-bold tracking-tight text-slate-100">🧠 AI Analysis</h2>
      <p class="mt-1 text-sm text-slate-400">Squad analyser → budget → replacements → fixtures → decision</p>
    </div>

    <Transition mode="out-in"
      enter-active-class="transition-all duration-200" enter-from-class="opacity-0 translate-y-1"
      leave-active-class="transition-all duration-200" leave-to-class="opacity-0">

      <!-- Loading state -->
      <div v-if="loading" key="loading"
        class="flex h-72 flex-col items-center justify-center">
        <div class="mb-5 flex gap-2">
          <span class="h-3 w-3 animate-bounce rounded-full bg-brand-purple"></span>
          <span class="h-3 w-3 animate-bounce rounded-full bg-brand-purple [animation-delay:150ms]"></span>
          <span class="h-3 w-3 animate-bounce rounded-full bg-brand-purple [animation-delay:300ms]"></span>
        </div>
        <p class="font-medium text-slate-100">Agents are analysing your squad…</p>
        <p class="mt-1 text-sm text-slate-400">This usually takes 15–30 seconds</p>
      </div>

      <!-- Error state -->
      <div v-else-if="error" key="error"
        class="rounded-xl border border-red-500/30 bg-red-500/10 p-4">
        <p class="font-medium text-red-300">{{ error }}</p>
      </div>

      <!-- Recommendation -->
      <div v-else-if="recommendation" key="recommendation"
        class="rounded-xl border border-brand-border border-l-4 border-l-brand-green bg-brand-dark/60 p-5">
        <div class="mb-4 flex items-center gap-3">
          <span class="flex h-8 w-8 items-center justify-center rounded-full bg-brand-green/15 text-brand-green">✓</span>
          <h3 class="text-lg font-bold tracking-tight text-slate-100">Transfer Recommendation</h3>
        </div>
        <p class="whitespace-pre-wrap leading-relaxed text-slate-200">{{ recommendation }}</p>
        <p class="mt-4 border-t border-brand-border pt-3 text-xs text-slate-500">
          ⚽ Generated at {{ generatedAt }}
        </p>
      </div>

      <!-- Empty state -->
      <div v-else key="empty"
        class="flex h-72 flex-col items-center justify-center text-center">
        <div class="mb-4 flex h-16 w-16 items-center justify-center rounded-2xl border border-brand-border bg-brand-dark/60 text-3xl">⚽</div>
        <p class="font-medium text-slate-100">No analysis yet</p>
        <p class="mt-1 max-w-xs text-sm text-slate-400">Enter your team details and click Get Recommendation</p>
      </div>
    </Transition>
  </div>
</template>

<script setup>
defineProps({
  recommendation: String,
  loading: Boolean,
  error: String,
  generatedAt: String
})
</script>
