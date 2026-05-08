import { useEffect, useState } from 'react';
import type { AppSettings } from '../../types';
import { confirmStrong } from '../../components/ui/ConfirmDialog';
import { exportBackup, parseBackup } from '../../lib/export/backup';
import { useFinanceStore } from '../transactions/store';

export function SettingsPage() {
  const store = useFinanceStore();
  const [settings, setSettings] = useState<AppSettings>(store.settings);

  useEffect(() => {
    setSettings(store.settings);
  }, [store.settings]);

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
  return <div className="space-y-6"><h1 className="text-3xl font-black">Configurações</h1><form onSubmit={save} className="card grid gap-4 p-5 md:grid-cols-2"><label><span className="label">Moeda padrão</span><select className="input" value={settings.currency} onChange={()=>setSettings({...settings,currency:'BRL'})}><option value="BRL">BRL — Real brasileiro</option></select></label><label><span className="label">Formato de data</span><select className="input" value={settings.dateFormat} onChange={()=>setSettings({...settings,dateFormat:'dd/MM/yyyy'})}><option value="dd/MM/yyyy">DD/MM/AAAA</option></select></label><label><span className="label">Tema</span><select className="input" value={settings.theme} onChange={(event)=>setSettings({...settings,theme:event.target.value as AppSettings['theme']})}><option value="system">Sistema</option><option value="light">Claro</option><option value="dark">Escuro</option></select></label><label><span className="label">Início do mês financeiro</span><input className="input" type="number" min={1} max={28} value={settings.financialMonthStart} onChange={(event)=>setSettings({...settings,financialMonthStart:Number(event.target.value)})}/></label><label><span className="label">Visão financeira</span><select className="input" value={settings.cashView} onChange={(event)=>setSettings({...settings,cashView:event.target.value as AppSettings['cashView']})}><option value="caixa">Caixa</option><option value="competencia">Competência</option></select></label><div className="flex items-end gap-2"><button className="btn btn-primary" type="submit">Salvar configurações</button></div></form><section className="card space-y-4 p-5"><h2 className="text-xl font-black">Privacidade, backup e dados</h2><p className="text-sm text-slate-600 dark:text-slate-300">Seus dados ficam salvos no IndexedDB deste navegador. Exporte backup antes de limpar dados ou trocar de dispositivo.</p><div className="flex flex-wrap gap-3"><button className="btn btn-secondary" onClick={()=>exportBackup(payload())}>Exportar backup</button><label className="btn btn-secondary cursor-pointer">Importar backup<input type="file" accept=".json" hidden onChange={(event)=>restore(event.target.files?.[0])}/></label><button className="btn btn-danger" onClick={()=>confirmStrong('Esta ação apagará todos os dados locais do navegador.') && store.wipe()}>Apagar todos os dados</button></div></section></div>;
}
