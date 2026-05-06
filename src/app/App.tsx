import { useEffect, useMemo, useState } from 'react';
import { AppLayout } from '../components/layout/AppLayout';
import { ErrorBoundary } from '../components/ui/ErrorBoundary';
import { DashboardPage } from '../features/dashboard/DashboardPage';
import { TransactionsPage } from '../features/transactions/TransactionsPage';
import { ImportPage } from '../features/importSpreadsheet/ImportPage';
import { CategoriesPage } from '../features/categories/CategoriesPage';
import { AccountsPage } from '../features/accounts/AccountsPage';
import { CreditCardsPage } from '../features/creditCards/CreditCardsPage';
import { BudgetsPage } from '../features/budgets/BudgetsPage';
import { GoalsPage } from '../features/goals/GoalsPage';
import { ReportsPage } from '../features/reports/ReportsPage';
import { BackupPage } from '../features/backup/BackupPage';
import { SettingsPage } from '../features/settings/SettingsPage';
import { useFinanceStore } from '../features/transactions/store';
function currentPath(){ return window.location.pathname === '/' ? '/dashboard' : window.location.pathname; }
export default function App() { const [path,setPath]=useState(currentPath()); const [dark,setDark]=useState(false); const load=useFinanceStore(s=>s.load); const loading=useFinanceStore(s=>s.loading); const error=useFinanceStore(s=>s.error); useEffect(()=>{load();},[load]); useEffect(()=>{document.documentElement.classList.toggle('dark',dark);},[dark]); const navigate=(p:string)=>{history.pushState(null,'',p); setPath(p);}; useEffect(()=>{const fn=()=>setPath(currentPath()); window.addEventListener('popstate',fn); return()=>window.removeEventListener('popstate',fn);},[]); const Page=useMemo(()=>({ '/dashboard':DashboardPage, '/lancamentos':TransactionsPage, '/importar':ImportPage, '/categorias':CategoriesPage, '/contas':AccountsPage, '/cartoes':CreditCardsPage, '/orcamentos':BudgetsPage, '/metas':GoalsPage, '/relatorios':ReportsPage, '/backups':BackupPage, '/configuracoes':SettingsPage }[path] ?? DashboardPage),[path]); return <ErrorBoundary><AppLayout current={path} navigate={navigate} dark={dark} toggleDark={()=>setDark(v=>!v)}>{loading && <div className="card mb-4 p-4 font-bold">Carregando dados locais...</div>}{error && <div className="mb-4 rounded-2xl bg-rose-100 p-4 font-bold text-rose-800">{error}</div>}<Page/></AppLayout></ErrorBoundary>; }
