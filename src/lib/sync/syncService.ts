import { deleteOne, putMany, putOne } from '../db/localDb';
import { getStoredSession, supabaseFetch } from '../supabase/client';
import { clearSyncOperation, markSyncOperationAttempt, readSyncQueue } from './syncQueue';
import { dedupeTransactionsByIdentity, mergeByLatest } from './merge';
import type { MigrationSummary, SyncCollection, SyncEntityMap, SyncOperation, SyncSnapshot } from './syncTypes';

const tableByCollection: Record<SyncCollection, string> = {
  transactions: 'transactions',
  categories: 'categories',
  accounts: 'accounts',
  creditCards: 'credit_cards',
  budgets: 'budgets',
  goals: 'goals',
  settings: 'settings',
};

const collections: SyncCollection[] = ['transactions', 'categories', 'accounts', 'creditCards', 'budgets', 'goals', 'settings'];

type RemoteRow<T> = { id: string; user_id: string; data: T; created_at: string; updated_at: string; deleted_at: string | null; source: string | null };

function updatedAtOf(value: unknown): string {
  const item = value as { updatedAt?: string; createdAt?: string };
  return item.updatedAt ?? item.createdAt ?? new Date().toISOString();
}

function withSyncMetadata<T extends { id?: string }>(item: T): T {
  const now = new Date().toISOString();
  return { ...item, id: item.id ?? 'settings', updatedAt: updatedAtOf(item) ?? now } as T;
}

async function upsertRemote<K extends SyncCollection>(collection: K, item: SyncEntityMap[K]): Promise<void> {
  const session = getStoredSession();
  if (!session?.user.id) throw new Error('Sessão expirada. Entre novamente.');
  const record = withSyncMetadata(item as SyncEntityMap[K] & { id?: string });
  const body = { id: record.id, user_id: session.user.id, data: record, updated_at: updatedAtOf(record), deleted_at: null, source: 'web' };
  await supabaseFetch(`/rest/v1/${tableByCollection[collection]}?on_conflict=user_id,id`, {
    method: 'POST',
    headers: { Prefer: 'resolution=merge-duplicates,return=representation' },
    body: JSON.stringify(body),
  });
}

async function softDeleteRemote(collection: SyncCollection, id: string): Promise<void> {
  const session = getStoredSession();
  if (!session?.user.id) throw new Error('Sessão expirada. Entre novamente.');
  await supabaseFetch(`/rest/v1/${tableByCollection[collection]}?id=eq.${encodeURIComponent(id)}&user_id=eq.${encodeURIComponent(session.user.id)}`, {
    method: 'PATCH',
    body: JSON.stringify({ deleted_at: new Date().toISOString(), updated_at: new Date().toISOString() }),
  });
}

export async function pushOperation(operation: SyncOperation): Promise<void> {
  if (operation.action === 'delete') await softDeleteRemote(operation.collection, operation.recordId);
  else if (operation.payload) await upsertRemote(operation.collection, operation.payload as never);
}

export async function flushSyncQueue(): Promise<number> {
  let synced = 0;
  for (const operation of readSyncQueue()) {
    try {
      await pushOperation(operation);
      clearSyncOperation(operation.id);
      synced += 1;
    } catch (error) {
      markSyncOperationAttempt(operation.id);
      console.warn('Operação ficou pendente para nova tentativa.', error);
      throw error;
    }
  }
  return synced;
}

async function fetchCollection<K extends SyncCollection>(collection: K): Promise<SyncEntityMap[K][]> {
  const rows = await supabaseFetch<RemoteRow<SyncEntityMap[K]>[]>(`/rest/v1/${tableByCollection[collection]}?select=id,data,updated_at,deleted_at&deleted_at=is.null`);
  return rows.map((row) => row.data).filter(Boolean);
}

async function fetchDeletedIds(collection: SyncCollection): Promise<string[]> {
  const rows = await supabaseFetch<Array<{ id: string }>>(`/rest/v1/${tableByCollection[collection]}?select=id&deleted_at=not.is.null`);
  return rows.map((row) => row.id);
}

export async function fetchRemoteDeletedIds(): Promise<Record<SyncCollection, string[]>> {
  const [transactions, categories, accounts, creditCards, budgets, goals, settings] = await Promise.all([
    fetchDeletedIds('transactions'),
    fetchDeletedIds('categories'),
    fetchDeletedIds('accounts'),
    fetchDeletedIds('creditCards'),
    fetchDeletedIds('budgets'),
    fetchDeletedIds('goals'),
    fetchDeletedIds('settings'),
  ]);
  return { transactions, categories, accounts, creditCards, budgets, goals, settings };
}

export async function fetchRemoteSnapshot(): Promise<SyncSnapshot> {
  const [transactions, categories, accounts, creditCards, budgets, goals, settings] = await Promise.all([
    fetchCollection('transactions'),
    fetchCollection('categories'),
    fetchCollection('accounts'),
    fetchCollection('creditCards'),
    fetchCollection('budgets'),
    fetchCollection('goals'),
    fetchCollection('settings'),
  ]);
  return { transactions, categories, accounts, creditCards, budgets, goals, settings };
}

export function mergeSnapshots(local: SyncSnapshot, remote: SyncSnapshot): SyncSnapshot {
  return {
    transactions: dedupeTransactionsByIdentity(mergeByLatest(local.transactions, remote.transactions)),
    categories: mergeByLatest(local.categories, remote.categories),
    accounts: mergeByLatest(local.accounts, remote.accounts),
    creditCards: mergeByLatest(local.creditCards, remote.creditCards),
    budgets: mergeByLatest(local.budgets, remote.budgets),
    goals: mergeByLatest(local.goals, remote.goals),
    settings: mergeByLatest(local.settings, remote.settings),
  };
}

export async function persistSnapshot(snapshot: SyncSnapshot): Promise<void> {
  await Promise.all([
    putMany('transactions', snapshot.transactions),
    putMany('categories', snapshot.categories),
    putMany('accounts', snapshot.accounts),
    putMany('creditCards', snapshot.creditCards),
    putMany('budgets', snapshot.budgets),
    putMany('goals', snapshot.goals),
    snapshot.settings[0] ? putOne('settings', snapshot.settings[0]) : Promise.resolve(),
  ]);
}

export async function migrateLocalSnapshot(snapshot: SyncSnapshot): Promise<MigrationSummary> {
  for (const collection of collections) {
    const items = snapshot[collection] as SyncEntityMap[typeof collection][];
    for (const item of items) await upsertRemote(collection, item as never);
  }
  return {
    transactions: snapshot.transactions.length,
    categories: snapshot.categories.length,
    accounts: snapshot.accounts.length,
    creditCards: snapshot.creditCards.length,
    budgets: snapshot.budgets.length,
    goals: snapshot.goals.length,
    settings: snapshot.settings.length,
  };
}

export async function deleteLocalByRemoteDeleted(rows: Array<{ collection: SyncCollection; id: string }>): Promise<void> {
  await Promise.all(rows.map((row) => deleteOne(row.collection, row.id)));
}
