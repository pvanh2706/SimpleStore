import { apiRequest } from './client'
import type { C14AttentionKind, C14ExperimentEventInput, C14ExperimentEventType } from './types'

export async function recordC14EventBestEffort(
  eventType: C14ExperimentEventType,
  productId: string | null = null,
  attentionKind: C14AttentionKind | null = null,
  eventId = crypto.randomUUID(),
) {
  const input: C14ExperimentEventInput = { eventId, eventType, productId, attentionKind }
  try {
    await apiRequest('/api/experiments/c14/events', {
      method: 'POST',
      body: JSON.stringify(input),
    })
  } catch (reason) {
    console.debug('C14 experiment event was not recorded.', { eventType, eventId, reason })
  }
  return eventId
}
