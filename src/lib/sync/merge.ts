import type { Transaction } from '../../types';
import type { SyncCollection, SyncEntity, SyncSnapshot } from './syncTypes';

function timestampOf(item: SyncEntity): string {
  const candidate = item as { updatedAt?: string; createdAt?: string; id?: string };
  return candidate.updatedAt ?? candidate.createdAt ?? '1970-01-01T00:00:00.000Z';
}

function transactionIdentity(transaction: Transaction): string {
  if (transaction.externalId) return `external:${transaction.externalId.trim().toLocaleLowerCase('pt-BR')}`;
  return `natural:${transaction.date}|${transaction.description.trim().toLocaleLowerCase('pt-BR')}|${transaction.value}`;
}

export function mergeByLatest<T extends { id?: string }>(local: T[], remote: T[]): T[] {
  const map = new Map<string, T>();
  for (const item of local) if (item.id) map.set(item.id, item);
  for (const item of remote) {
    if (!item.id) continue;
    const current = map.get(item.id);
    if (!current || timestampOf(item as unknown as SyncEntity) >= timestampOf(current as unknown as SyncEntity)) {
      if (current && timestampOf(item as unknown as SyncEntity) !== timestampOf(current as unknown as SyncEntity)) console.info('Conflito resolvido por updatedAt mais recente:', item.id);
      map.set(item.id, item);
    }
  }
  return [...map.values()];
}

export function dedupeByIdAndExternalId<T extends { id: string; externalId?: string }>(items: T[]): T[] {
  const seenIds = new Set<string>();
  const seenExternal = new Set<string>();
  const result: T[] = [];
  for (const item of items) {
    if (seenIds.has(item.id)) continue;
    if (item.externalId && seenExternal.has(item.externalId)) continue;
    seenIds.add(item.id);
    if (item.externalId) seenExternal.add(item.externalId);
    result.push(item);
  }
  return result;
}

export function dedupeTransactionsByIdentity(transactions: Transaction[]): Transaction[] {
  const byIdentity = new Map<string, Transaction>();
  for (const transaction of transactions) {
    const key = transactionIdentity(transaction);
    const current = byIdentity.get(key);
    if (!current || timestampOf(transaction) >= timestampOf(current)) byIdentity.set(key, transaction);
  }
  return dedupeByIdAndExternalId([...byIdentity.values()]);
}

export function removeDeletedFromSnapshot(snapshot: SyncSnapshot, deleted: Record<SyncCollection, string[]>): SyncSnapshot {
  return {
    transactions: snapshot.transactions.filter((item) => !deleted.transactions.includes(item.id)),
    categories: snapshot.categories.filter((item) => !deleted.categories.includes(item.id)),
    accounts: snapshot.accounts.filter((item) => !deleted.accounts.includes(item.id)),
    creditCards: snapshot.creditCards.filter((item) => !deleted.creditCards.includes(item.id)),
    budgets: snapshot.budgets.filter((item) => !deleted.budgets.includes(item.id)),
    goals: snapshot.goals.filter((item) => !deleted.goals.includes(item.id)),
    settings: snapshot.settings.filter((item) => !item.id || !deleted.settings.includes(item.id)),
  };
}
