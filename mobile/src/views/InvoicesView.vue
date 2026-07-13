<template>
  <div class="page">
    <header class="page-header rise">
      <div>
        <h1>الفواتير</h1>
        <p>سجل تفعيلات الاشتراك</p>
      </div>
    </header>

    <div v-if="store.loading" class="list">
      <div v-for="n in 4" :key="n" class="skeleton item-skel" />
    </div>

    <div v-else-if="store.error" class="alert alert-error">{{ store.error }}</div>

    <div v-else-if="!store.invoices.length" class="empty surface">
      <strong>لا توجد فواتير</strong>
      <span>ستظهر هنا عمليات التفعيل عند توفرها</span>
    </div>

    <div v-else class="list">
      <article
        v-for="(item, index) in store.invoices"
        :key="item.id || index"
        class="item surface rise"
        :style="{ animationDelay: `${index * 0.04}s` }"
      >
        <div class="item-top">
          <strong>{{ item.profile || `فاتورة #${item.id}` }}</strong>
          <span class="badge" :class="item.saleType ? 'cash' : 'credit'">
            {{ item.saleType ? 'نقدي' : 'آجل' }}
          </span>
        </div>
        <div class="item-bottom">
          <span>{{ formatDateTime(item.creationDate) }}</span>
          <span class="amount num">{{ formatMoney(item.cost) }}</span>
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

onMounted(() => store.loadInvoices())
</script>

<style scoped>
.list {
  display: grid;
  gap: 12px;
}

.item {
  padding: 16px;
}

.item-top,
.item-bottom {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
}

.item-top strong {
  font-size: 0.98rem;
}

.item-bottom {
  margin-top: 12px;
  color: var(--ink-muted);
  font-size: 0.86rem;
}

.amount {
  color: var(--ink);
  font-weight: 700;
  font-size: 1rem;
}

.badge {
  font-size: 0.75rem;
  font-weight: 700;
  padding: 6px 10px;
  border-radius: 999px;
}

.badge.cash {
  background: var(--accent-soft);
  color: var(--accent-deep);
}

.badge.credit {
  background: var(--warn-soft);
  color: var(--warn);
}

.item-skel {
  height: 92px;
}
</style>
