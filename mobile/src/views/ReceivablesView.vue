<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>المقبوضات</h1>
        <p>سجل الدفعات المستلمة</p>
      </div>
    </header>

    <div v-if="store.loading" class="list">
      <div v-for="n in 4" :key="n" class="skeleton item-skel" />
    </div>

    <div v-else-if="store.error" class="alert alert-error">{{ store.error }}</div>

    <div v-else-if="!store.receivables.length" class="empty surface">
      <strong>لا توجد مقبوضات</strong>
      <span>ستظهر هنا عمليات الدفع عند تسجيلها</span>
    </div>

    <div v-else class="list">
      <article
        v-for="(item, index) in store.receivables"
        :key="item.id || index"
        class="item surface rise"
        :style="{ animationDelay: `${index * 0.04}s` }"
      >
        <div class="thumb">#{{ item.id }}</div>
        <div class="meta">
          <strong class="num">{{ formatMoney(item.amount) }}</strong>
          <span>{{ formatDateTime(item.creationDate) }}</span>
        </div>
      </article>
    </div>
  </div>
</template>

<script setup>
import { onMounted } from 'vue'
import { useSubscriberStore } from '../stores/subscriber'
import { formatMoney, formatDateTime } from '../composables/format'

const store = useSubscriberStore()

onMounted(() => store.loadReceivables())
</script>

<style scoped>
.list {
  display: grid;
  gap: 12px;
}

.item {
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px;
}

.thumb {
  width: 48px;
  height: 48px;
  border-radius: 14px;
  display: grid;
  place-items: center;
  background: var(--warn-soft);
  color: var(--warn);
  font-weight: 800;
  font-size: 0.78rem;
  font-family: var(--font-num);
}

.meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.meta strong {
  font-size: 1.05rem;
}

.meta span {
  color: var(--ink-muted);
  font-size: 0.86rem;
}

.item-skel {
  height: 80px;
}
</style>
