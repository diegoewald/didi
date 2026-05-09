import { createId } from '../utils/id';
import { deleteOne, putMany, putOne } from '../db/localDb';
import { getStoredSession, isSupabaseConfigured, summarizeSupabaseResponse, supabaseFetch, SupabaseRequestError } from '../supabase/client';
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

export interface MigrationFailureContext {
  collection: SyncCollection;
  table: string;
  operation: string;
  itemId: string;
  status?: number;
  response?: unknown;
}

export class MigrationSyncError extends Error {
  context: MigrationFailureContext;

  constructor(message: string, context: MigrationFailureContext, cause?: unknown) {
    super(message, { cause });
    this.name = 'MigrationSyncError';
    this.context = context;
  }
}

export interface SupabaseSyncDiagnosticResult {
  configured: boolean;
  loggedIn: boolean;
  userId?: string;
  checks: Array<{ step: string; ok: boolean; detail: string }>;
}


function incrementMigrationSummary(summary: MigrationSummary, collection: SyncCollection): void {
  switch (collection) {
    case 'transactions': summary.transactions += 1; break;
    case 'categories': summary.categories += 1; break;
    case 'accounts': summary.accounts += 1; break;
    case 'creditCards': summary.creditCards += 1; break;
    case 'budgets': summary.budgets += 1; break;
    case 'goals': summary.goals += 1; break;
    case 'settings': summary.settings += 1; break;
  }
}

function updatedAtOf(value: unknown): string {
  const item = value as { updatedAt?: string; createdAt?: string };
  return item.updatedAt ?? item.createdAt ?? new Date().toISOString();
}

function withSyncMetadata<T extends { id?: string }>(item: T): T {
  return { ...item, id: item.id ?? 'settings', updatedAt: updatedAtOf(item) } as T;
}

function itemPreview(item: unknown): Record<string, unknown> {
  const candidate = item as { id?: string; externalId?: string; date?: string; type?: string; updatedAt?: string; name?: string; category?: string };
  return {
    id: candidate.id,
    externalId: candidate.externalId,
    date: candidate.date,
    type: candidate.type,
    updatedAt: candidate.updatedAt,
    name: candidate.name,
    category: candidate.category,
  };
}

function isMissingCompositeConstraint(error: unknown): boolean {
  if (!(error instanceof SupabaseRequestError)) return false;
  const summary = summarizeSupabaseResponse(error.details.response).toLocaleLowerCase('pt-BR');
  return summary.includes('no unique') || summary.includes('on conflict') || summary.includes('42p10');
}

function toMigrationSyncError<K extends SyncCollection>(collection: K, operation: string, item: SyncEntityMap[K], error: unknown): MigrationSyncError {
  const table = tableByCollection[collection];
  const record = withSyncMetadata(item as SyncEntityMap[K] & { id?: string });
  const status = error instanceof SupabaseRequestError ? error.details.status : undefined;
  const response = error instanceof SupabaseRequestError ? error.details.response : error instanceof Error ? error.message : error;
  const context = { collection, table, operation, itemId: record.id ?? 'sem-id', status, response };
  console.error('Falha detalhada na migração Supabase:', { ...context, item: itemPreview(record) });
  return new MigrationSyncError(`Falha ao migrar ${collection} (${context.itemId}): ${summarizeSupabaseResponse(response)}`, context, error);
}

async function upsertRemote<K extends SyncCollection>(collection: K, item: SyncEntityMap[K]): Promise<void> {
  const session = getStoredSession();
  if (!session?.user.id) throw new Error('Sessão expirada. Entre novamente.');
  const table = tableByCollection[collection];
  const record = withSyncMetadata(item as SyncEntityMap[K] & { id?: string });
  const body = { id: record.id, user_id: session.user.id, data: record, updated_at: updatedAtOf(record), deleted_at: null, source: 'web' };
  console.info('Migrando registro para Supabase:', { collection, table, operation: 'upsert', item: itemPreview(record) });
  try {
    await supabaseFetch(`/rest/v1/${table}?on_conflict=user_id,id`, {
      method: 'POST',
      headers: { Prefer: 'resolution=merge-duplicates,return=representation' },
      body: JSON.stringify(body),
    });
  } catch (error) {
    if (isMissingCompositeConstraint(error)) {
      console.warn('Schema Supabase parece antigo/sem constraint (user_id,id). Tentando fallback on_conflict=id. Reaplique supabase/schema.sql da V1.6.2.', { collection, table, item: itemPreview(record), error });
      try {
        await supabaseFetch(`/rest/v1/${table}?on_conflict=id`, {
          method: 'POST',
          headers: { Prefer: 'resolution=merge-duplicates,return=representation' },
          body: JSON.stringify(body),
        });
        return;
      } catch (fallbackError) {
        throw toMigrationSyncError(collection, 'upsert:fallback-id', item, fallbackError);
      }
    }
    throw toMigrationSyncError(collection, 'upsert', item, error);
  }
}

async function softDeleteRemote(collection: SyncCollection, id: string): Promise<void> {
  const session = getStoredSession();
  if (!session?.user.id) throw new Error('Sessão expirada. Entre novamente.');
  const table = tableByCollection[collection];
  await supabaseFetch(`/rest/v1/${table}?id=eq.${encodeURIComponent(id)}&user_id=eq.${encodeURIComponent(session.user.id)}`, {
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
  const summary: MigrationSummary = { transactions: 0, categories: 0, accounts: 0, creditCards: 0, budgets: 0, goals: 0, settings: 0 };
  for (const collection of collections) {
    const items = snapshot[collection] as SyncEntityMap[typeof collection][];
    console.info('Iniciando migração da collection:', { collection, table: tableByCollection[collection], total: items.length });
    for (const item of items) {
      await upsertRemote(collection, item as never);
      incrementMigrationSummary(summary, collection);
    }
  }
  return summary;
}

export async function runSupabaseSyncDiagnostic(): Promise<SupabaseSyncDiagnosticResult> {
  const session = getStoredSession();
  const checks: SupabaseSyncDiagnosticResult['checks'] = [];
  const result: SupabaseSyncDiagnosticResult = { configured: isSupabaseConfigured(), loggedIn: Boolean(session), userId: session?.user.id, checks };
  if (!result.configured) {
    checks.push({ step: 'configuração', ok: false, detail: 'VITE_SUPABASE_URL ou VITE_SUPABASE_ANON_KEY ausentes.' });
    return result;
  }
  if (!session?.user.id) {
    checks.push({ step: 'sessão', ok: false, detail: 'Nenhum usuário logado.' });
    return result;
  }
  checks.push({ step: 'sessão', ok: true, detail: `Usuário autenticado: ${session.user.id}` });
  const diagnosticId = `diag-${createId()}`;
  const body = { id: diagnosticId, user_id: session.user.id, data: { kind: 'diagnostic', createdAt: new Date().toISOString() }, source: 'diagnostic', deleted_at: null };
  try {
    await supabaseFetch('/rest/v1/sync_queue?on_conflict=user_id,id', {
      method: 'POST',
      headers: { Prefer: 'resolution=merge-duplicates,return=representation' },
      body: JSON.stringify(body),
    });
    checks.push({ step: 'insert sync_queue', ok: true, detail: 'Insert/upsert de diagnóstico funcionou.' });
    const rows = await supabaseFetch<Array<{ id: string }>>(`/rest/v1/sync_queue?select=id&id=eq.${encodeURIComponent(diagnosticId)}&user_id=eq.${encodeURIComponent(session.user.id)}`);
    checks.push({ step: 'select sync_queue', ok: rows.some((row) => row.id === diagnosticId), detail: `${rows.length} registro(s) retornado(s).` });
    await supabaseFetch(`/rest/v1/sync_queue?id=eq.${encodeURIComponent(diagnosticId)}&user_id=eq.${encodeURIComponent(session.user.id)}`, {
      method: 'PATCH',
      body: JSON.stringify({ deleted_at: new Date().toISOString(), updated_at: new Date().toISOString() }),
    });
    checks.push({ step: 'soft delete sync_queue', ok: true, detail: 'Soft delete de diagnóstico funcionou.' });
  } catch (error) {
    const detail = error instanceof SupabaseRequestError ? `HTTP ${error.details.status}: ${summarizeSupabaseResponse(error.details.response)}` : error instanceof Error ? error.message : 'Erro desconhecido';
    checks.push({ step: 'diagnóstico sync_queue', ok: false, detail });
    console.error('Diagnóstico Supabase falhou:', error);
  }
  return result;
}

export function migrationErrorMessage(error: unknown): string {
  if (error instanceof MigrationSyncError) {
    const status = error.context.status ? `HTTP ${error.context.status}` : 'sem status HTTP';
    return `Não foi possível migrar seus dados para a conta online. Seus dados locais continuam seguros. Collection: ${error.context.collection}. Tabela: ${error.context.table}. Item: ${error.context.itemId}. Detalhe técnico: ${status} — ${summarizeSupabaseResponse(error.context.response)}.`;
  }
  if (error instanceof Error) return `Não foi possível migrar seus dados para a conta online. Seus dados locais continuam seguros. Detalhe técnico: ${error.message}`;
  return 'Não foi possível migrar seus dados para a conta online. Seus dados locais continuam seguros. Veja o console para detalhes técnicos.';
}

export async function deleteLocalByRemoteDeleted(rows: Array<{ collection: SyncCollection; id: string }>): Promise<void> {
  await Promise.all(rows.map((row) => deleteOne(row.collection, row.id)));
}
