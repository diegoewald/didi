import { useState } from 'react';
import { LockKeyhole, Upload } from 'lucide-react';
import { confirmStrong } from '../../components/ui/ConfirmDialog';
import { exportBackup, exportEncryptedBackup, parseBackup } from '../../lib/export/backup';
import { encryptedBackupUnavailableMessage, isEncryptedBackupSupported } from '../../lib/export/cryptoBackup';
import { useFinanceStore } from '../transactions/store';

export function BackupPage() {
  const store = useFinanceStore();
  const [password, setPassword] = useState('');
  const encryptedSupported = isEncryptedBackupSupported();
  const payload = () => ({
    version: 1 as const,
    exportedAt: new Date().toISOString(),
    transactions: store.transactions,
    categories: store.categories,
    accounts: store.accounts,
    creditCards: store.creditCards,
    budgets: store.budgets,
    goals: store.goals,
    settings: store.settings,
  });

  const restore = async (file?: File) => {
    if (!file) return;
    try {
      const maybePassword = password || window.prompt('Se este backup for criptografado, informe a senha. Para backup simples, deixe em branco.') || undefined;
      await store.restore(await parseBackup(file, maybePassword));
      alert('Backup restaurado com sucesso.');
    } catch (error) {
      alert(error instanceof Error ? `Erro ao importar backup: ${error.message}` : 'Backup inválido ou dados corrompidos.');
    }
  };

  const encrypted = async () => {
    if (!encryptedSupported) {
      alert(encryptedBackupUnavailableMessage);
      return;
    }
    try {
      await exportEncryptedBackup(payload(), password);
      alert('Backup criptografado gerado com sucesso. Guarde a senha: ela não pode ser recuperada.');
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Erro ao criptografar backup.');
    }
  };

  return (
    <div className="space-y-6">
      <h1 className="text-3xl font-black">Backup e restauração</h1>
      <section className="card grid gap-4 p-5 md:grid-cols-2 xl:grid-cols-4">
        <button className="btn btn-primary" onClick={() => exportBackup(payload())}>Exportar backup JSON</button>
        <label className="btn btn-secondary cursor-pointer"><Upload size={18} />Importar backup<input type="file" accept=".json" hidden onChange={(event) => restore(event.target.files?.[0])} /></label>
        <input className="input" type="password" value={password} onChange={(event) => setPassword(event.target.value)} placeholder="Senha para backup criptografado" />
        <button className="btn btn-secondary" onClick={encrypted} title={encryptedSupported ? 'Exportar backup criptografado' : encryptedBackupUnavailableMessage}><LockKeyhole size={18} />Exportar criptografado</button>
        <button className="btn btn-danger xl:col-span-4" onClick={() => confirmStrong('Esta ação apagará todos os dados locais do navegador.') && store.wipe()}>Apagar todos os dados</button>
      </section>
      <section className="rounded-3xl bg-emerald-50 p-5 text-emerald-900 dark:bg-emerald-950 dark:text-emerald-100">
        <b>Privacidade local-first:</b> os dados ficam no IndexedDB do navegador. Backup simples é legível; backup criptografado usa PBKDF2 + AES-GCM no próprio navegador e exige a senha para restauração. Em HTTP por IP local, alguns navegadores bloqueiam criptografia avançada; nesse caso use backup simples ou HTTPS.
      </section>
      {!encryptedSupported && <section className="rounded-3xl p-5 text-sm font-bold alert-warning">{encryptedBackupUnavailableMessage}</section>}
    </div>
  );
}
