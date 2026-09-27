<template>
  <div class="relative min-h-screen overflow-hidden bg-brand-dark">
    <!-- Soft background glows so the glass cards have something to blur -->
    <div class="pointer-events-none absolute -top-40 -left-40 h-[28rem] w-[28rem] rounded-full bg-brand-purple/20 blur-3xl"></div>
    <div class="pointer-events-none absolute top-1/2 -right-40 h-[24rem] w-[24rem] rounded-full bg-brand-green/10 blur-3xl"></div>

    <!-- Header -->
    <header class="relative border-b border-brand-border bg-gradient-to-r from-brand-purple/40 via-brand-purple/10 to-brand-dark">
      <div class="container mx-auto flex items-center justify-between gap-4 px-6 py-5">
        <div class="flex items-center gap-3">
          <div class="flex h-10 w-10 items-center justify-center rounded-xl bg-brand-purple text-xl shadow-lg shadow-brand-purple/30">⚽</div>
          <div>
            <h1 class="text-xl font-bold tracking-tight text-slate-100">FPL AI Manager</h1>
            <p class="text-sm text-slate-400">Powered by Microsoft Agent Framework</p>
          </div>
        </div>

        <div class="flex items-center gap-2 rounded-full border border-brand-green/30 bg-brand-green/10 px-3 py-1.5 text-sm font-semibold text-brand-green">
          <span class="h-2 w-2 rounded-full bg-brand-green"></span>
          GW {{ gameWeek || '–' }}
        </div>
      </div>
    </header>

    <!-- Main content -->
    <main class="container relative mx-auto px-6 py-8">
      <div class="grid grid-cols-1 gap-6 lg:grid-cols-3">

        <!-- Left panel -->
        <div class="space-y-6 lg:col-span-1">
          <TransferForm
            v-model:gameWeek="gameWeek"
            @recommendation="handleRecommendation"
            @loading="handleLoading"
            @error="handleError" />
          <PlayerSearch />
        </div>

        <!-- Right panel -->
        <div class="lg:col-span-2">
          <AgentResponse
            :recommendation="recommendation"
            :loading="loading"
            :error="error"
            :generatedAt="generatedAt" />
        </div>

      </div>
    </main>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import TransferForm from './components/TransferForm.vue'
import PlayerSearch from './components/PlayerSearch.vue'
import AgentResponse from './components/AgentResponse.vue'

const gameWeek = ref(6)
const recommendation = ref(null)
const loading = ref(false)
const error = ref(null)
const generatedAt = ref(null)

const handleRecommendation = (data) => {
  recommendation.value = data.recommendation
  generatedAt.value = new Date(data.generatedAt).toLocaleString()
  loading.value = false
}

const handleLoading = (val) => {
  loading.value = val
  error.value = null
  recommendation.value = null
}

const handleError = (err) => {
  error.value = err
  loading.value = false
}
</script>
