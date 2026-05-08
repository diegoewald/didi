import { describe, expect, it, beforeEach } from 'vitest';
import { clearSyncQueue, enqueueSyncOperation, readSyncQueue, writeSyncQueue } from '../lib/sync/syncQueue';
import { mergeByLatest, dedupeByIdAndExternalId } from '../lib/sync/merge';

describe('sync merge and queue', () => {
  beforeEach(() => clearSyncQueue());

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

  it('salva fila pendente no localStorage', () => {
    enqueueSyncOperation('transactions', 'delete', 'tx-1');
    expect(readSyncQueue()).toHaveLength(1);
    writeSyncQueue([]);
    expect(readSyncQueue()).toHaveLength(0);
  });
});
