import { Cloud, CloudOff, Loader2, WifiOff } from 'lucide-react';
import { useAuthStore } from '../auth/authStore';

const label = {
  local: 'Modo local',
  online: 'Online',
  syncing: 'Sincronizando',
  synced: 'Sincronizado',
  error: 'Erro de sync',
  offline: 'Offline',
  migration_pending: 'Migração pendente',
  migration_done: 'Migração concluída',
};

export function SyncStatus() {
  const { configured, session, status, message } = useAuthStore();
  const icon = status === 'syncing' ? <Loader2 className="animate-spin" size={15} /> : status === 'offline' ? <WifiOff size={15} /> : session ? <Cloud size={15} /> : <CloudOff size={15} />;
  const text = !configured ? 'Modo local' : label[status];
  return <div className="rounded-full border border-slate-200 bg-white/90 px-3 py-2 text-xs font-black text-slate-700 shadow-sm dark:border-slate-800 dark:bg-slate-950/80 dark:text-slate-200" title={message ?? (session?.user.email || 'Dados locais no IndexedDB')}><span className="flex items-center gap-2">{icon}{text}</span></div>;
}
