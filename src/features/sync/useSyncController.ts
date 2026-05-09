import { useCallback, useEffect } from 'react';
import { isSupabaseConfigured } from '../../lib/supabase/client';
import { fetchRemoteDeletedIds, fetchRemoteSnapshot, flushSyncQueue, mergeSnapshots, migrateLocalSnapshot } from '../../lib/sync/syncService';
import { removeDeletedFromSnapshot } from '../../lib/sync/merge';
import { clearSyncQueue } from '../../lib/sync/syncQueue';
import { exportBackup } from '../../lib/export/backup';
import {
  formatMigrationSummary,
  getMigrationDecision,
  hasMigratableLocalData,
  prepareMigrationSnapshot,
  setMigrationDecision,
  summarizeSnapshot,
} from '../../lib/sync/migration';
import type { MigrationSummary } from '../../lib/sync/syncTypes';
import { useAuthStore } from '../auth/authStore';
import { useFinanceStore } from '../transactions/store';

export function useSyncController({ auto = true }: { auto?: boolean } = {}) {
  const session = useAuthStore((state) => state.session);
  const loadSession = useAuthStore((state) => state.loadSession);
  const setStatus = useAuthStore((state) => state.setStatus);
  const snapshot = useFinanceStore((state) => state.snapshot);
  const applySnapshot = useFinanceStore((state) => state.applySnapshot);
  const settings = useFinanceStore((state) => state.settings);

  useEffect(() => {
    loadSession();
  }, [loadSession]);

  const hasPendingMigration = useCallback(() => {
    if (!session) return false;
    return hasMigratableLocalData(snapshot()) && getMigrationDecision(session.user.id) === null;
  }, [session, snapshot]);

  const syncNow = useCallback(async () => {
    if (!isSupabaseConfigured() || !session) {
      setStatus('local', 'Modo local ativo. Dados salvos apenas neste dispositivo.');
      return;
    }
    if (!navigator.onLine) {
      setStatus('offline', 'Você está offline. A sincronização será tentada novamente depois.');
      return;
    }
    if (hasPendingMigration()) {
      setStatus('migration_pending', 'Encontramos dados salvos neste dispositivo. Escolha se deseja migrar para sua conta online antes de sincronizar.');
      return;
    }
    setStatus('syncing', 'Sincronizando dados...');
    try {
      await flushSyncQueue();
      const [remote, deleted] = await Promise.all([fetchRemoteSnapshot(), fetchRemoteDeletedIds()]);
      const localWithoutDeleted = removeDeletedFromSnapshot(snapshot(), deleted);
      const merged = mergeSnapshots(localWithoutDeleted, remote);
      await applySnapshot(merged);
      setStatus('synced', 'Dados sincronizados com sua conta online.');
    } catch (error) {
      console.error('Erro de sincronização:', error);
      setStatus('error', error instanceof Error ? error.message : 'Não foi possível sincronizar agora. Seus dados continuam salvos neste dispositivo.');
    }
  }, [applySnapshot, hasPendingMigration, session, setStatus, snapshot]);

  const migrateLocal = useCallback(async (): Promise<MigrationSummary | null> => {
    if (!session) {
      setStatus('local', 'Entre na conta para migrar dados locais.');
      return null;
    }
    const local = snapshot();
    const before = summarizeSnapshot(local);
    const confirmed = confirm(`Encontramos dados salvos neste dispositivo. Recomendamos exportar um backup antes de migrar. Deseja enviar para sua conta online?\n\nResumo local: ${formatMigrationSummary(before)}.`);
    if (!confirmed) {
      setStatus('migration_pending', 'Migração cancelada. Seus dados continuam somente neste dispositivo até você decidir.');
      return null;
    }
    setStatus('syncing', 'Migrando dados locais para a conta online...');
    try {
      const [remote, deleted] = await Promise.all([fetchRemoteSnapshot(), fetchRemoteDeletedIds()]);
      const localWithoutDeleted = removeDeletedFromSnapshot(local, deleted);
      const prepared = prepareMigrationSnapshot(localWithoutDeleted, remote);
      const summary = await migrateLocalSnapshot(prepared);
      await applySnapshot(prepared);
      clearSyncQueue();
      setMigrationDecision(session.user.id, 'migrated');
      setStatus('migration_done', `Migração concluída: ${formatMigrationSummary(summary)} enviados/confirmados. Nenhum dado local foi apagado.`);
      return summary;
    } catch (error) {
      console.error('Erro ao migrar dados:', error);
      setStatus('error', 'Erro ao migrar dados. Nenhum dado local foi apagado.');
      return null;
    }
  }, [applySnapshot, session, setStatus, snapshot]);

  const keepLocalOnly = useCallback(() => {
    if (!session) return;
    clearSyncQueue();
    setMigrationDecision(session.user.id, 'local-only');
    setStatus('online', 'Dados locais mantidos apenas neste dispositivo. Novas alterações online poderão sincronizar normalmente.');
  }, [session, setStatus]);

  const cancelMigration = useCallback(() => {
    if (!session) return;
    setStatus('migration_pending', 'Migração cancelada. Nenhum dado foi enviado; escolha migrar ou manter local antes de sincronizar.');
  }, [session, setStatus]);

  const exportBackupBeforeSync = useCallback(() => {
    const local = snapshot();
    exportBackup({ version: 1, exportedAt: new Date().toISOString(), transactions: local.transactions, categories: local.categories, accounts: local.accounts, creditCards: local.creditCards, budgets: local.budgets, goals: local.goals, settings });
  }, [settings, snapshot]);

  useEffect(() => {
    if (!auto || !session) return;
    if (hasPendingMigration()) setStatus('migration_pending', 'Encontramos dados salvos neste dispositivo. Deseja enviar esses dados para sua conta online?');
    void syncNow();
    const interval = window.setInterval(() => void syncNow(), 60_000);
    const online = () => void syncNow();
    window.addEventListener('online', online);
    return () => {
      window.clearInterval(interval);
      window.removeEventListener('online', online);
    };
  }, [auto, hasPendingMigration, session, setStatus, syncNow]);

  return { syncNow, migrateLocal, keepLocalOnly, cancelMigration, exportBackupBeforeSync };
}
