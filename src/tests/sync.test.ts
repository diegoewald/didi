import { beforeEach, describe, expect, it } from 'vitest';
import { isSupabaseConfigured, resolveSupabaseConfig, supabaseNotConfiguredMessage } from '../lib/supabase/client';
import { clearSyncQueue, compactSyncQueue, enqueueSyncOperation, readSyncQueue, writeSyncQueue } from '../lib/sync/syncQueue';
import { dedupeByIdAndExternalId, dedupeTransactionsByIdentity, mergeByLatest, removeDeletedFromSnapshot } from '../lib/sync/merge';
import { hasMigratableLocalData, prepareMigrationSnapshot, summarizeSnapshot } from '../lib/sync/migration';
import type { SyncSnapshot } from '../lib/sync/syncTypes';
import type { Transaction } from '../types';

const emptySnapshot: SyncSnapshot = { transactions: [], categories: [], accounts: [], creditCards: [], budgets: [], goals: [], settings: [] };

function tx(partial: Partial<Transaction>): Transaction {
  return {
    id: 'tx-1',
    date: '2026-05-09',
    description: 'Mercado',
    type: 'Despesa',
    category: 'Alimentação',
    value: '100.00',
    account: 'Banco',
    paymentMethod: 'Pix',
    status: 'Pago',
    installment: 1,
    totalInstallments: 1,
    createdAt: '2026-05-09T10:00:00.000Z',
    updatedAt: '2026-05-09T10:00:00.000Z',
    ...partial,
  };
}

describe('sync config, migration, merge and queue', () => {
  beforeEach(() => clearSyncQueue());

  it('detecta modo local sem Supabase configurado', () => {
    const config = resolveSupabaseConfig({ VITE_SUPABASE_URL: '', VITE_SUPABASE_ANON_KEY: undefined });
    expect(isSupabaseConfigured(config)).toBe(false);
    expect(supabaseNotConfiguredMessage).toContain('Modo local ativo');
  });

  it('detecta Supabase configurado e normaliza barra final da URL', () => {
    const config = resolveSupabaseConfig({ VITE_SUPABASE_URL: 'https://projeto.supabase.co/', VITE_SUPABASE_ANON_KEY: 'anon' });
    expect(config.url).toBe('https://projeto.supabase.co');
    expect(isSupabaseConfigured(config)).toBe(true);
  });

  it('mantém a versão mais recente por updatedAt', () => {
    const merged = mergeByLatest(
      [{ id: 't1', description: 'local', updatedAt: '2026-05-01T10:00:00.000Z' }],
      [{ id: 't1', description: 'remote', updatedAt: '2026-05-02T10:00:00.000Z' }],
    );
    expect(merged).toEqual([{ id: 't1', description: 'remote', updatedAt: '2026-05-02T10:00:00.000Z' }]);
  });

  it('deduplica por id e externalId', () => {
    const result = dedupeByIdAndExternalId([
      { id: '1', externalId: 'A' },
      { id: '1', externalId: 'B' },
      { id: '2', externalId: 'A' },
      { id: '3', externalId: 'C' },
    ]);
    expect(result.map((item) => item.id)).toEqual(['1', '3']);
  });

  it('deduplica lançamentos por externalId e por data+descrição+valor usando updatedAt', () => {
    const result = dedupeTransactionsByIdentity([
      tx({ id: 'old', externalId: 'EXT-1', updatedAt: '2026-05-01T10:00:00.000Z' }),
      tx({ id: 'new', externalId: 'EXT-1', updatedAt: '2026-05-02T10:00:00.000Z' }),
      tx({ id: 'n1', externalId: undefined, description: 'Padaria', value: '20.00', updatedAt: '2026-05-01T10:00:00.000Z' }),
      tx({ id: 'n2', externalId: undefined, description: 'padaria ', value: '20.00', updatedAt: '2026-05-03T10:00:00.000Z' }),
    ]);
    expect(result.map((item) => item.id).sort()).toEqual(['n2', 'new']);
  });

  it('compacta fila para não reenviar operações duplicadas do mesmo registro', () => {
    const compacted = compactSyncQueue([
      { id: 'op-1', collection: 'transactions', action: 'create', recordId: 'tx-1', updatedAt: '2026-05-01T10:00:00.000Z', attempts: 0 },
      { id: 'op-2', collection: 'transactions', action: 'delete', recordId: 'tx-1', updatedAt: '2026-05-01T10:01:00.000Z', attempts: 0 },
    ]);
    expect(compacted).toHaveLength(1);
    expect(compacted[0]).toMatchObject({ id: 'op-1', action: 'delete', recordId: 'tx-1' });
  });

  it('salva fila pendente no localStorage', () => {
    enqueueSyncOperation('transactions', 'delete', 'tx-1');
    expect(readSyncQueue()).toHaveLength(1);
    writeSyncQueue([]);
    expect(readSyncQueue()).toHaveLength(0);
  });

  it('detecta dados migráveis e prepara migração sem sobrescrever remoto mais novo', () => {
    const local = { ...emptySnapshot, transactions: [tx({ id: 'tx-1', updatedAt: '2026-05-01T10:00:00.000Z' })] };
    const remote = { ...emptySnapshot, transactions: [tx({ id: 'tx-1', description: 'Remoto', updatedAt: '2026-05-02T10:00:00.000Z' })] };
    expect(hasMigratableLocalData(local)).toBe(true);
    expect(summarizeSnapshot(local).transactions).toBe(1);
    expect(prepareMigrationSnapshot(local, remote).transactions[0].description).toBe('Remoto');
  });

  it('remove do snapshot registros marcados como deleted_at no remoto', () => {
    const snapshot: SyncSnapshot = { ...emptySnapshot, transactions: [tx({ id: 'tx-1' }), tx({ id: 'tx-2' })] };
    const result = removeDeletedFromSnapshot(snapshot, { transactions: ['tx-1'], categories: [], accounts: [], creditCards: [], budgets: [], goals: [], settings: [] });
    expect(result.transactions.map((item) => item.id)).toEqual(['tx-2']);
  });
});
