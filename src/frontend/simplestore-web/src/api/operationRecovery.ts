import { ApiError } from './client'
import type { OperationStatus } from './types'

export function isAmbiguousOperationFailure(reason: unknown): boolean {
  return !(reason instanceof ApiError)
    || reason.status === 408
    || reason.status >= 500
    || reason.problem.code === 'operation-lock-timeout'
}

export async function recoverCompletedOperation<T>(
  operationId: string,
  expectedOperationType: string,
  checkOperation: (operationId: string) => Promise<OperationStatus | null>,
  loadResult: (resultReference: string) => Promise<T>,
): Promise<T | null> {
  const operation = await checkOperation(operationId)
  if (operation?.status !== 'Completed'
    || operation.operationType !== expectedOperationType
    || !operation.resultReference) return null

  return loadResult(operation.resultReference)
}

export function problemMessage(
  reason: unknown,
  messages: Record<string, string>,
  fallback: string,
): string {
  if (!(reason instanceof ApiError)) return reason instanceof Error ? reason.message : fallback
  const code = reason.problem.code
  return code && messages[code] ? messages[code] : reason.message
}
