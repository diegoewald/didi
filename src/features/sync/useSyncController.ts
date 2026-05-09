import { useCallback, useEffect } from 'react';
import { isSupabaseConfigured } from '../../lib/supabase/client';
import { fetchRemoteDeletedIds, fetchRemoteSnapshot, flushSyncQueue, mergeSnapshots, migrateLocalSnapshot } from '../../lib/sync/syncService';
import { removeDeletedFromSnapshot } from '../../lib/sync/merge';
import type { MigrationSummary } from '../../lib/sync/syncTypes';
import { exportBackup } from '../../lib/export/backup';
import { useAuthStore } from '../auth/authStore';
import { useFinanceStore } from '../transactions/store';

export function useSyncController() {
  const session = useAuthStore((state) => state.session);
  const loadSession = useAuthStore((state) => state.loadSession);
  const setStatus = useAuthStore((state) => state.setStatus);
  const snapshot = useFinanceStore((state) => state.snapshot);
  const applySnapshot = useFinanceStore((state) => state.applySnapshot);
  const settings = useFinanceStore((state) => state.settings);

  useEffect(() => {
    loadSession();
  }, [loadSession]);

  const syncNow = useCallback(async () => {
    if (!isSupabaseConfigured() || !session) {
      setStatus('local', 'Modo local ativo. Dados salvos apenas neste dispositivo.');
      return;
    }
    if (!navigator.onLine) {
      setStatus('offline', 'Você está offline. A sincronização será tentada novamente depois.');
      return;
    }
    setStatus('syncing', 'Sincronizando dados...');
    try {
      await flushSyncQueue();
      const [remote, deleted] = await Promise.all([fetchRemoteSnapshot(), fetchRemoteDeletedIds()]);
      const localWithoutDeleted = removeDeletedFromSnapshot(snapshot(), deleted);
      const merged = mergeSnapshots(localWithoutDeleted, remote);
      await applySnapshot(merged);
      await migrateLocalSnapshot(merged);
      setStatus('synced', 'Dados sincronizados com sua conta online.');
    } catch (error) {
      console.error('Erro de sincronização:', error);
      setStatus('error', error instanceof Error ? error.message : 'Não foi possível sincronizar agora. Seus dados continuam salvos neste dispositivo.');
    }
  }, [applySnapshot, session, setStatus, snapshot]);

  const migrateLocal = useCallback(async (): Promise<MigrationSummary | null> => {
    if (!session) {
      setStatus('local', 'Entre na conta para migrar dados locais.');
      return null;
    }
    const confirmed = confirm('Encontramos dados salvos neste dispositivo. Exporte um backup antes de migrar. Deseja enviar esses dados para sua conta online agora?');
    if (!confirmed) return null;
    setStatus('syncing', 'Migrando dados locais para a conta online...');
    try {
      const summary = await migrateLocalSnapshot(snapshot());
      await syncNow();
      setStatus('synced', `Migração concluída: ${summary.transactions} lançamentos, ${summary.categories} categorias, ${summary.accounts} contas, ${summary.creditCards} cartões, ${summary.goals} metas e ${summary.budgets} orçamentos enviados.`);
      return summary;
    } catch (error) {
      console.error('Erro ao migrar dados:', error);
      setStatus('error', 'Erro ao migrar dados. Nenhum dado local foi apagado.');
      return null;
    }
  }, [session, setStatus, snapshot, syncNow]);

  const exportBackupBeforeSync = useCallback(() => {
    const local = snapshot();
    exportBackup({ version: 1, exportedAt: new Date().toISOString(), transactions: local.transactions, categories: local.categories, accounts: local.accounts, creditCards: local.creditCards, budgets: local.budgets, goals: local.goals, settings });
  }, [settings, snapshot]);

  useEffect(() => {
    if (!session) return;
    void syncNow();
    const interval = window.setInterval(() => void syncNow(), 60_000);
    const online = () => void syncNow();
    window.addEventListener('online', online);
    return () => {
      window.clearInterval(interval);
      window.removeEventListener('online', online);
    };
  }, [session, syncNow]);

  return { syncNow, migrateLocal, exportBackupBeforeSync };
}
