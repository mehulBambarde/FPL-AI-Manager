<template>
  <div class="rounded-2xl border border-brand-border bg-brand-card/60 p-6 shadow-xl shadow-black/20 backdrop-blur-xl">
    <div class="mb-5">
      <h2 class="text-lg font-bold tracking-tight text-slate-100">🔁 Transfer Advisor</h2>
      <p class="mt-1 text-sm text-slate-400">Five AI agents weigh up your squad, budget and fixtures.</p>
    </div>

    <div class="space-y-4">
      <div class="grid grid-cols-2 gap-3">
        <div>
          <label for="teamId" class="text-xs font-medium uppercase tracking-wide text-slate-400">Team ID</label>
          <input id="teamId" v-model.number="form.teamId" type="number" :class="inputClass" />
        </div>
        <div>
          <label for="gameWeek" class="text-xs font-medium uppercase tracking-wide text-slate-400">Gameweek</label>
          <input id="gameWeek" v-model.number="gameWeek" type="number" min="1" max="38" :class="inputClass" />
        </div>
      </div>

      <div class="grid grid-cols-2 gap-3">
        <div>
          <label for="budget" class="text-xs font-medium uppercase tracking-wide text-slate-400">Budget (£m)</label>
          <input id="budget" v-model.number="form.budget" type="number" step="0.1" :class="inputClass" />
        </div>
        <div>
          <label for="transfers" class="text-xs font-medium uppercase tracking-wide text-slate-400">Free Transfers</label>
          <select id="transfers" v-model.number="form.transfersAvailable" :class="inputClass">
            <option :value="1">1</option>
            <option :value="2">2</option>
          </select>
        </div>
      </div>

      <label for="willingToTakeHit"
        class="flex cursor-pointer items-center justify-between rounded-lg border border-brand-border bg-brand-dark/60 px-3 py-2.5 transition-all duration-200 hover:border-brand-purple/50">
        <span>
          <span class="block text-sm font-medium text-slate-100">Willing to take a hit</span>
          <span class="block text-xs text-slate-400">Allow an extra transfer for −4 points</span>
        </span>
        <input id="willingToTakeHit" v-model="form.willingToTakeHit" type="checkbox"
          class="h-4 w-4 accent-brand-purple" />
      </label>

      <button @click="getRecommendation"
        :disabled="loading"
        class="w-full rounded-lg bg-brand-purple px-4 py-2.5 font-semibold text-white shadow-lg shadow-brand-purple/25
               transition-all duration-200 hover:-translate-y-0.5 hover:bg-violet-500 hover:shadow-brand-purple/40
               disabled:translate-y-0 disabled:cursor-not-allowed disabled:opacity-50">
        {{ loading ? 'Analysing…' : 'Get Recommendation ⚽' }}
      </button>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import axios from 'axios'

const emit = defineEmits(['recommendation', 'loading', 'error'])

// Shared with the header's gameweek badge
const gameWeek = defineModel('gameWeek', { default: 6 })

const inputClass =
  'mt-1.5 w-full rounded-lg border border-brand-border bg-brand-dark/60 px-3 py-2 text-slate-100 ' +
  'outline-none transition-all duration-200 focus:border-brand-purple focus:ring-2 focus:ring-brand-purple/30'

const form = ref({
  teamId: 6804083,
  budget: 1.6,
  transfersAvailable: 1,
  willingToTakeHit: false
})

const loading = ref(false)

const getRecommendation = async () => {
  loading.value = true
  emit('loading', true)

  try {
    const response = await axios.post('/api/transfer/recommend', {
      teamId: form.value.teamId,
      gameWeek: gameWeek.value,
      budget: form.value.budget,
      transfersAvailable: form.value.transfersAvailable,
      willingToTakeHit: form.value.willingToTakeHit
    })
    emit('recommendation', response.data)
  } catch (err) {
    console.error('Recommendation failed', err)
    emit('error', 'Something went wrong. Please try again.')
  } finally {
    loading.value = false
  }
}
</script>
