import { useMemo, useState } from 'react';
import { Search } from 'lucide-react';
import type { Transaction, TransactionFilters } from '../../types';
import { EmptyState } from '../../components/ui/EmptyState';
import { DataTable } from '../../components/tables/DataTable';
import { TransactionForm } from './TransactionForm';
import { useFinanceStore } from './store';

const currentMonth = new Date().toISOString().slice(5, 7);
const currentYear = new Date().getFullYear().toString();

export function TransactionsPage() {
  const { transactions, categories, accounts, upsertTransaction, deleteTransaction } = useFinanceStore();
  const [editing, setEditing] = useState<Transaction | null>(null);
  const [filters, setFilters] = useState<TransactionFilters>({
    month: currentMonth,
    year: currentYear,
    account: '',
    category: '',
    type: '',
    status: '',
    search: '',
  });

  const rows = useMemo(
    () =>
      transactions
        .filter((transaction) => {
          const [year, month] = transaction.date.split('-');
          return (
            (!filters.search || `${transaction.description} ${transaction.category} ${transaction.account}`.toLowerCase().includes(filters.search.toLowerCase())) &&
            (!filters.month || month === filters.month) &&
            (!filters.year || year === filters.year) &&
            (!filters.account || transaction.account === filters.account) &&
            (!filters.category || transaction.category === filters.category) &&
            (!filters.type || transaction.type === filters.type) &&
            (!filters.status || transaction.status === filters.status)
          );
        })
        .sort((a, b) => b.date.localeCompare(a.date)),
    [transactions, filters],
  );

  const update = (key: keyof TransactionFilters) => (event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>) =>
    setFilters({ ...filters, [key]: event.target.value });

  const duplicate = async (transaction: Transaction) => {
    const now = new Date().toISOString();
    await upsertTransaction({
      ...transaction,
      id: crypto.randomUUID(),
      description: `${transaction.description} (cópia)`,
      externalId: undefined,
      createdAt: now,
      updatedAt: now,
    });
  };

  const markPaid = async (transaction: Transaction) => {
    await upsertTransaction({
      ...transaction,
      status: 'Pago',
      paidAt: new Date().toISOString().slice(0, 10),
      updatedAt: new Date().toISOString(),
    });
  };

  const remove = async (transaction: Transaction) => {
    if (window.confirm(`Excluir o lançamento "${transaction.description}"?`)) await deleteTransaction(transaction.id);
  };

  return (
    <div className="space-y-6">
      <div className="card p-5 sm:p-6">
        <p className="text-xs font-black uppercase tracking-[.2em] text-teal-700 dark:text-teal-300">Movimentações</p>
        <h1 className="mt-1 text-3xl font-black tracking-[-.05em] sm:text-4xl">Lançamentos</h1>
        <p className="mt-2 max-w-2xl text-sm font-medium leading-6 text-slate-600 dark:text-slate-300 dark:text-slate-400">Cadastre, edite, duplique, filtre e marque movimentações como pagas.</p>
      </div>
      <TransactionForm categories={categories} editing={editing} onCancelEdit={() => setEditing(null)} onSave={(transaction) => { upsertTransaction(transaction); setEditing(null); }} />
      <section className="card p-5 sm:p-6">
        <div className="mb-5 grid gap-3 rounded-3xl bg-slate-50/70 p-3 dark:bg-slate-900/45 md:grid-cols-4 xl:grid-cols-7">
          <div className="relative md:col-span-2">
            <Search className="absolute left-3 top-3 text-slate-400" size={18} />
            <input className="input pl-10" value={filters.search} onChange={update('search')} placeholder="Buscar descrição, categoria ou conta" />
          </div>
          <select className="input" value={filters.month} onChange={update('month')}><option value="">Todos meses</option>{Array.from({ length: 12 }, (_, i) => String(i + 1).padStart(2, '0')).map((month) => <option key={month} value={month}>{month}</option>)}</select>
          <input className="input" value={filters.year} onChange={update('year')} placeholder="Ano" inputMode="numeric" />
          <select className="input" value={filters.account} onChange={update('account')}><option value="">Todas contas</option>{accounts.map((account) => <option key={account.id}>{account.name}</option>)}</select>
          <select className="input" value={filters.category} onChange={update('category')}><option value="">Todas categorias</option>{categories.map((category) => <option key={category.id}>{category.name}</option>)}</select>
          <select className="input" value={filters.type} onChange={update('type')}><option value="">Todos tipos</option><option>Receita</option><option>Despesa</option><option>Transferência</option></select>
          <select className="input" value={filters.status} onChange={update('status')}><option value="">Todos status</option><option>Pago</option><option>Pendente</option><option>Atrasado</option><option>Cancelado</option></select>
        </div>
        {rows.length ? (
          <DataTable rows={rows} onEdit={setEditing} onDelete={remove} onDuplicate={duplicate} onMarkPaid={markPaid} />
        ) : (
          <EmptyState title="Nenhum lançamento encontrado" description="Ajuste os filtros, importe uma planilha ou cadastre uma movimentação manual para começar." />
        )}
      </section>
    </div>
  );
}
