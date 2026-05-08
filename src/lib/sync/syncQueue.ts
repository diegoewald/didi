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
  localStorage.setItem(queueKey, JSON.stringify(queue));
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
