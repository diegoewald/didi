import { createId } from '../utils/id';
import type { SyncCollection, SyncEntityMap, SyncOperation } from './syncTypes';

const queueKey = 'financaspro-sync-queue';

export function readSyncQueue(): SyncOperation[] {
  try {
    const raw = localStorage.getItem(queueKey);
    return raw ? (JSON.parse(raw) as SyncOperation[]) : [];
  } catch (error) {
    console.warn('Fila de sincronização inválida reiniciada.', error);
    localStorage.removeItem(queueKey);
    return [];
  }
}

export function writeSyncQueue(queue: SyncOperation[]): void {
  localStorage.setItem(queueKey, JSON.stringify(compactSyncQueue(queue)));
}

export function compactSyncQueue(queue: SyncOperation[]): SyncOperation[] {
  const compacted = new Map<string, SyncOperation>();
  for (const operation of queue) {
    const key = `${operation.collection}:${operation.recordId}`;
    const previous = compacted.get(key);
    if (!previous) {
      compacted.set(key, operation);
      continue;
    }
    compacted.set(key, {
      ...operation,
      id: previous.id,
      attempts: Math.max(previous.attempts, operation.attempts),
      updatedAt: operation.updatedAt >= previous.updatedAt ? operation.updatedAt : previous.updatedAt,
    });
  }
  return [...compacted.values()].sort((a, b) => a.updatedAt.localeCompare(b.updatedAt));
}

export function enqueueSyncOperation<K extends SyncCollection>(collection: K, action: SyncOperation<K>['action'], recordId: string, payload?: SyncEntityMap[K]): SyncOperation<K> {
  const operation: SyncOperation<K> = { id: createId(), collection, action, recordId, payload, updatedAt: new Date().toISOString(), attempts: 0 };
  writeSyncQueue([...readSyncQueue(), operation]);
  return operation;
}

export function clearSyncOperation(id: string): void {
  writeSyncQueue(readSyncQueue().filter((operation) => operation.id !== id));
}

export function markSyncOperationAttempt(id: string): void {
  writeSyncQueue(readSyncQueue().map((operation) => operation.id === id ? { ...operation, attempts: operation.attempts + 1 } : operation));
}

export function clearSyncQueue(): void {
  writeSyncQueue([]);
}
