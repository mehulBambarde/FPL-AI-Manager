<template>
  <div class="rounded-2xl border border-brand-border bg-brand-card/60 p-6 shadow-xl shadow-black/20 backdrop-blur-xl">
    <div class="mb-5">
      <h2 class="text-lg font-bold tracking-tight text-slate-100">🔍 Player Search</h2>
      <p class="mt-1 text-sm text-slate-400">Ask about past gameweeks in plain English.</p>
    </div>

    <div class="flex gap-2">
      <input v-model="query"
        placeholder="e.g. Haaland home games last season"
        class="min-w-0 flex-1 rounded-lg border border-brand-border bg-brand-dark/60 px-3 py-2 text-sm text-slate-100
               placeholder:text-slate-500 outline-none transition-all duration-200
               focus:border-brand-green focus:ring-2 focus:ring-brand-green/30"
        @keyup.enter="search" />
      <button @click="search"
        :disabled="loading"
        class="rounded-lg bg-brand-green px-4 py-2 text-sm font-semibold text-white transition-all duration-200
               hover:bg-emerald-400 disabled:cursor-not-allowed disabled:opacity-50">
        {{ loading ? '…' : 'Search' }}
      </button>
    </div>

    <p v-if="error" class="mt-4 text-sm text-red-400">{{ error }}</p>

    <p v-else-if="searched && !loading && !results.length"
      class="mt-4 text-sm text-slate-500">
      No matches found.
    </p>

    <TransitionGroup v-if="results.length" tag="div" class="mt-4 space-y-2"
      enter-active-class="transition-all duration-200" enter-from-class="opacity-0 translate-y-1">
      <div v-for="(result, index) in results" :key="result.text + index"
        :class="['rounded-lg border border-l-4 border-brand-border bg-brand-dark/60 p-3 transition-all duration-200 hover:bg-brand-dark', matchStyle(result.score).border]">
        <p class="text-sm leading-relaxed text-slate-100">{{ result.text }}</p>
        <div class="mt-2 flex items-center gap-2">
          <span :class="['rounded-full px-2 py-0.5 text-xs font-semibold', matchStyle(result.score).pill]">
            {{ (result.score * 100).toFixed(1) }}% match
          </span>
          <span class="text-xs text-slate-500">{{ matchStyle(result.score).label }}</span>
        </div>
      </div>
    </TransitionGroup>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const query = ref('')
const results = ref([])
const loading = ref(false)
const error = ref(null)
const searched = ref(false)

// Cosine similarity from text-embedding-3-small tops out around 0.6 for good matches,
// so the thresholds sit much lower than you might expect.
const matchStyle = (score) => {
  if (score >= 0.6) return { border: 'border-l-brand-green', pill: 'bg-brand-green/15 text-brand-green', label: 'Strong match' }
  if (score >= 0.5) return { border: 'border-l-amber-400', pill: 'bg-amber-400/15 text-amber-300', label: 'Good match' }
  return { border: 'border-l-slate-500', pill: 'bg-slate-500/15 text-slate-400', label: 'Loose match' }
}

const search = async () => {
  if (!query.value.trim()) return
  loading.value = true
  error.value = null

  try {
    const response = await axios.get('/api/players/search', {
      params: { query: query.value, topN: 5 }
    })
    results.value = response.data
  } catch (err) {
    console.error('Search failed', err)
    results.value = []
    error.value = 'Search failed. Please try again.'
  } finally {
    loading.value = false
    searched.value = true
  }
}
</script>
