import { useEffect, useState } from 'react';
import type { AppSettings } from '../../types';
import { confirmStrong } from '../../components/ui/ConfirmDialog';
import { exportBackup, parseBackup } from '../../lib/export/backup';
import { isSupabaseConfigured } from '../../lib/supabase/client';
import { formatMigrationSummary, summarizeSnapshot } from '../../lib/sync/migration';
import { useAuthStore } from '../auth/authStore';
import { useSyncController } from '../sync/useSyncController';
import { useFinanceStore } from '../transactions/store';

export function SettingsPage() {
  const store = useFinanceStore();
  const auth = useAuthStore();
  const { syncNow, migrateLocal, keepLocalOnly, cancelMigration, exportBackupBeforeSync } = useSyncController({ auto: false });
  const [settings, setSettings] = useState<AppSettings>(store.settings);

  useEffect(() => {
    setSettings(store.settings);
  }, [store.settings]);

  const localSummary = formatMigrationSummary(summarizeSnapshot(store.snapshot()));
  const payload = () => ({ version: 1 as const, exportedAt: new Date().toISOString(), transactions: store.transactions, categories: store.categories, accounts: store.accounts, creditCards: store.creditCards, budgets: store.budgets, goals: store.goals, settings: store.settings });
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    await store.upsertSettings({ ...settings, currency: 'BRL', dateFormat: 'dd/MM/yyyy' });
    alert('Configurações salvas com sucesso.');
  };
  const restore = async (file?: File) => {
    if (!file) return;
    try {
      const password = window.prompt('Se o backup for criptografado, informe a senha. Caso contrário deixe em branco.') || undefined;
      await store.restore(await parseBackup(file, password));
      alert('Backup importado com sucesso.');
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Erro ao importar backup.');
    }
  };

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-black">Configurações</h1>
      <section className="card space-y-4 p-5">
        <div className="flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between">
          <div>
            <h2 className="text-xl font-black">Conta e sincronização</h2>
            <p className="text-sm text-slate-600 dark:text-slate-300">{auth.session ? `Conectado como ${auth.session.user.email ?? 'usuário Supabase'}.` : 'Modo local ativo. Dados salvos apenas neste dispositivo.'}</p>
          </div>
          <span className="rounded-full px-3 py-2 text-xs font-black alert-success">{auth.session ? 'Modo online' : 'Modo local'}</span>
        </div>

        {auth.message && <div className="rounded-2xl p-3 text-sm font-bold alert-warning">{auth.message}</div>}
        {!isSupabaseConfigured() && <div className="rounded-2xl p-3 text-sm font-bold alert-warning">Modo local ativo. Configure o Supabase para sincronização online.</div>}
        {auth.status === 'migration_pending' && (
          <div className="rounded-3xl border border-amber-200 bg-amber-50 p-4 text-sm text-amber-950 dark:border-amber-900/70 dark:bg-amber-950/40 dark:text-amber-100">
            <p className="font-black">Encontramos dados salvos neste dispositivo. Deseja enviar esses dados para sua conta online?</p>
            <p className="mt-1">Resumo local: {localSummary}. Recomendamos exportar um backup antes de migrar.</p>
          </div>
        )}

        <div className="flex flex-wrap gap-3">
          {!auth.session ? (
            <button className="btn btn-primary" onClick={() => { history.pushState(null, '', '/login'); window.dispatchEvent(new PopStateEvent('popstate')); }}>Entrar para sincronizar</button>
          ) : (
            <>
              <button className="btn btn-primary" onClick={() => void syncNow()}>Sincronizar agora</button>
              <button className="btn btn-secondary" onClick={exportBackupBeforeSync}>Exportar backup antes de migrar</button>
              <button className="btn btn-secondary" onClick={() => void migrateLocal()}>Migrar dados locais para conta</button>
              <button className="btn btn-secondary" onClick={keepLocalOnly}>Manter apenas neste dispositivo</button>
              <button className="btn btn-secondary" onClick={cancelMigration}>Cancelar migração</button>
              <button className="btn btn-danger" onClick={() => void auth.logout()}>Sair da conta</button>
            </>
          )}
        </div>
        <p className="text-xs text-slate-500 dark:text-slate-400">A V1.6.2 não apaga o IndexedDB automaticamente. O backup local continua disponível e é recomendado antes de migrar dados.</p>
      </section>

      <form onSubmit={save} className="card grid gap-4 p-5 md:grid-cols-2">
        <label><span className="label">Moeda padrão</span><select className="input" value={settings.currency} onChange={() => setSettings({ ...settings, currency: 'BRL' })}><option value="BRL">BRL — Real brasileiro</option></select></label>
        <label><span className="label">Formato de data</span><select className="input" value={settings.dateFormat} onChange={() => setSettings({ ...settings, dateFormat: 'dd/MM/yyyy' })}><option value="dd/MM/yyyy">DD/MM/AAAA</option></select></label>
        <label><span className="label">Tema</span><select className="input" value={settings.theme} onChange={(event) => setSettings({ ...settings, theme: event.target.value as AppSettings['theme'] })}><option value="system">Sistema</option><option value="light">Claro</option><option value="dark">Escuro</option></select></label>
        <label><span className="label">Início do mês financeiro</span><input className="input" type="number" min={1} max={28} value={settings.financialMonthStart} onChange={(event) => setSettings({ ...settings, financialMonthStart: Number(event.target.value) })} /></label>
        <label><span className="label">Visão financeira</span><select className="input" value={settings.cashView} onChange={(event) => setSettings({ ...settings, cashView: event.target.value as AppSettings['cashView'] })}><option value="caixa">Caixa</option><option value="competencia">Competência</option></select></label>
        <div className="flex items-end gap-2"><button className="btn btn-primary" type="submit">Salvar configurações</button></div>
      </form>

      <section className="card space-y-4 p-5">
        <h2 className="text-xl font-black">Privacidade, backup e dados</h2>
        <p className="text-sm text-slate-600 dark:text-slate-300">Seus dados locais ficam salvos no IndexedDB deste navegador. Em modo online, o Supabase sincroniza por usuário com RLS e mantém cache local.</p>
        <div className="flex flex-wrap gap-3">
          <button className="btn btn-secondary" onClick={() => exportBackup(payload())}>Exportar backup</button>
          <label className="btn btn-secondary cursor-pointer">Importar backup<input type="file" accept=".json" hidden onChange={(event) => restore(event.target.files?.[0])} /></label>
          <button className="btn btn-danger" onClick={() => confirmStrong('Esta ação apagará todos os dados locais do navegador.') && store.wipe()}>Apagar todos os dados</button>
        </div>
      </section>
    </div>
  );
}
