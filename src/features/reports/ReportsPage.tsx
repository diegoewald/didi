import { useMemo, useState } from 'react';
import { DataTable } from '../../components/tables/DataTable';
import { categoryExpensePercentages, monthlyEvolution } from '../../lib/calculations/finance';
import { exportMonthlyReportXlsx, exportTransactionsCsv, exportTransactionsXlsx } from '../../lib/export/backup';
import { formatCurrency } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

export function ReportsPage() {
  const { transactions } = useFinanceStore();
  const [category, setCategory] = useState('');
  const [month, setMonth] = useState(new Date().toISOString().slice(0, 7));
  const rows = useMemo(() => transactions.filter((transaction) => !category || transaction.category === category), [transactions, category]);
  const monthlyRows = rows.filter((transaction) => transaction.date.startsWith(month));
  const categories = [...new Set(transactions.map((transaction) => transaction.category))];
  const evolution = monthlyEvolution(rows, 12);
  const categoryTotals = categoryExpensePercentages(rows);

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-3 md:flex-row md:items-end">
        <div>
          <h1 className="text-3xl font-black">Relatórios</h1>
          <p className="text-slate-600 dark:text-slate-300">Filtros por período, categoria, conta, status, forma de pagamento e palavra-chave.</p>
        </div>
        <div className="flex flex-wrap gap-2">
          <button className="btn btn-secondary" onClick={() => exportTransactionsXlsx(rows)}>Exportar Excel</button>
          <button className="btn btn-secondary" onClick={() => exportTransactionsCsv(rows)}>Exportar CSV</button>
          <button className="btn btn-primary" onClick={() => exportMonthlyReportXlsx(monthlyRows, month)}>Relatório mensal Excel</button>
        </div>
      </div>
      <section className="card p-5">
        <div className="grid gap-3 md:grid-cols-2">
          <select className="input" value={category} onChange={(event) => setCategory(event.target.value)}><option value="">Todas categorias</option>{categories.map((item) => <option key={item}>{item}</option>)}</select>
          <input className="input" type="month" value={month} onChange={(event) => setMonth(event.target.value)} />
        </div>
        <div className="mt-5 grid gap-4 md:grid-cols-3">{evolution.slice(-3).map((report) => <div key={report.month} className="rounded-2xl bg-slate-50 p-4 dark:bg-slate-900"><b>{report.month}</b><p>Receitas {formatCurrency(report.income)}</p><p>Despesas {formatCurrency(report.expense)}</p><p>Resultado {formatCurrency(report.net)}</p></div>)}</div>
        <div className="mt-5 grid gap-2 md:grid-cols-2">{categoryTotals.slice(0, 8).map((item) => <div key={item.category} className="flex justify-between rounded-2xl bg-slate-50 p-3 dark:bg-slate-900"><b>{item.category}</b><span>{formatCurrency(item.value)} • {item.percentage.toFixed(1)}%</span></div>)}</div>
      </section>
      <DataTable rows={rows} />
    </div>
  );
}
