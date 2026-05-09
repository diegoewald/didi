import { beforeEach, describe, expect, it } from 'vitest';
import { isSupabaseConfigured, resolveSupabaseConfig, supabaseNotConfiguredMessage } from '../lib/supabase/client';
import { clearSyncQueue, compactSyncQueue, enqueueSyncOperation, readSyncQueue, writeSyncQueue } from '../lib/sync/syncQueue';
import { dedupeByIdAndExternalId, mergeByLatest, removeDeletedFromSnapshot } from '../lib/sync/merge';
import type { SyncSnapshot } from '../lib/sync/syncTypes';

describe('sync config, merge and queue', () => {
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

  it('compacta fila para não reenviar operações duplicadas do mesmo registro', () => {
    const compacted = compactSyncQueue([
      { id: 'op-1', collection: 'transactions', action: 'upsert', recordId: 'tx-1', updatedAt: '2026-05-01T10:00:00.000Z', attempts: 0 },
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

  it('remove do snapshot registros marcados como deleted_at no remoto', () => {
    const snapshot: SyncSnapshot = { transactions: [{ id: 'tx-1' } as never, { id: 'tx-2' } as never], categories: [], accounts: [], creditCards: [], budgets: [], goals: [], settings: [] };
    const result = removeDeletedFromSnapshot(snapshot, { transactions: ['tx-1'], categories: [], accounts: [], creditCards: [], budgets: [], goals: [], settings: [] });
    expect(result.transactions.map((item) => item.id)).toEqual(['tx-2']);
  });
});
