import type { SyncEntity } from './syncTypes';

function timestampOf(item: SyncEntity): string {
  const candidate = item as { updatedAt?: string; createdAt?: string; id?: string };
  return candidate.updatedAt ?? candidate.createdAt ?? '1970-01-01T00:00:00.000Z';
}

export function mergeByLatest<T extends { id?: string }>(local: T[], remote: T[]): T[] {
  const map = new Map<string, T>();
  for (const item of local) if (item.id) map.set(item.id, item);
  for (const item of remote) {
    if (!item.id) continue;
    const current = map.get(item.id);
    if (!current || timestampOf(item as unknown as SyncEntity) >= timestampOf(current as unknown as SyncEntity)) map.set(item.id, item);
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
