import { useEffect, useMemo, useState } from 'react';
import type { ComponentType } from 'react';
import { AppLayout } from '../components/layout/AppLayout';
import { ErrorBoundary } from '../components/ui/ErrorBoundary';
import { AccountsPage } from '../features/accounts/AccountsPage';
import { BackupPage } from '../features/backup/BackupPage';
import { BudgetsPage } from '../features/budgets/BudgetsPage';
import { CategoriesPage } from '../features/categories/CategoriesPage';
import { CreditCardsPage } from '../features/creditCards/CreditCardsPage';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { GoalsPage } from '../features/goals/GoalsPage';
import { ImportPage } from '../features/importSpreadsheet/ImportPage';
import { MonthlySummaryPage } from '../features/monthlySummary/MonthlySummaryPage';
import { ReportsPage } from '../features/reports/ReportsPage';
import { SettingsPage } from '../features/settings/SettingsPage';
import { AuthPage } from '../features/auth/AuthPage';
import { useSyncController } from '../features/sync/useSyncController';
import { TransactionsPage } from '../features/transactions/TransactionsPage';
import { useFinanceStore } from '../features/transactions/store';

const pages: Record<string, ComponentType> = {
  '/dashboard': DashboardPage,
  '/lancamentos': TransactionsPage,
  '/importar': ImportPage,
  '/categorias': CategoriesPage,
  '/contas': AccountsPage,
  '/cartoes': CreditCardsPage,
  '/orcamentos': BudgetsPage,
  '/metas': GoalsPage,
  '/resumo-mensal': MonthlySummaryPage,
  '/relatorios': ReportsPage,
  '/backups': BackupPage,
  '/configuracoes': SettingsPage,
  '/login': AuthPage,
};

function currentPath(): string {
  return window.location.pathname === '/' ? '/dashboard' : window.location.pathname;
}

export default function App() {
  const [path, setPath] = useState(currentPath());
  const [manualDark, setManualDark] = useState(false);
  const load = useFinanceStore((state) => state.load);
  const loading = useFinanceStore((state) => state.loading);
  const error = useFinanceStore((state) => state.error);
  const settings = useFinanceStore((state) => state.settings);
  const upsertSettings = useFinanceStore((state) => state.upsertSettings);
  useSyncController();

  useEffect(() => {
    load();
  }, [load]);

  const dark = settings.theme === 'system' ? manualDark : settings.theme === 'dark';

  useEffect(() => {
    const media = window.matchMedia('(prefers-color-scheme: dark)');
    const sync = () => setManualDark(media.matches);
    sync();
    media.addEventListener('change', sync);
    return () => media.removeEventListener('change', sync);
  }, []);

  useEffect(() => {
    document.documentElement.classList.toggle('dark', dark);
  }, [dark]);

  useEffect(() => {
    const listener = () => setPath(currentPath());
    window.addEventListener('popstate', listener);
    return () => window.removeEventListener('popstate', listener);
  }, []);

  const navigate = (nextPath: string) => {
    history.pushState(null, '', nextPath);
    setPath(nextPath);
  };

  const Page = useMemo(() => pages[path] ?? DashboardPage, [path]);

  return (
    <ErrorBoundary>
      <AppLayout current={path} navigate={navigate} dark={dark} toggleDark={() => upsertSettings({ ...settings, theme: dark ? 'light' : 'dark' })}>
        <div className="mb-4 rounded-3xl p-4 text-sm font-bold alert-success">
          Modo local-first: sem login, seus dados ficam no IndexedDB deste navegador. Com Supabase configurado e login, a sincronização online é opcional e mantém o cache local.
        </div>
        {loading && <div className="card mb-4 p-4 font-bold">Carregando dados locais...</div>}
        {error && <div className="mb-4 rounded-2xl p-4 font-bold alert-error">{error}</div>}
        <Page />
      </AppLayout>
    </ErrorBoundary>
  );
}
