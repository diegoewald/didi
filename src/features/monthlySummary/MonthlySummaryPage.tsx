import { useMemo, useState } from 'react';
import { ArrowDownCircle, ArrowUpCircle, Crown, Wallet } from 'lucide-react';
import { Cell, Pie, PieChart, ResponsiveContainer, Tooltip } from 'recharts';
import { ChartCard } from '../../components/ui/ChartCard';
import { StatCard } from '../../components/ui/StatCard';
import { DataTable } from '../../components/tables/DataTable';
import { categoryExpensePercentages, sumExpenses, sumIncome } from '../../lib/calculations/finance';
import { exportMonthlyReportXlsx } from '../../lib/export/backup';
import { formatCurrency } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

export function MonthlySummaryPage() {
  const { transactions } = useFinanceStore();
  const [month, setMonth] = useState(new Date().toISOString().slice(0, 7));
  const rows = useMemo(() => transactions.filter((transaction) => transaction.date.startsWith(month)), [transactions, month]);
  const income = sumIncome(rows);
  const expense = sumExpenses(rows);
  const net = (Number(income) - Number(expense)).toFixed(2);
  const categories = categoryExpensePercentages(rows);
  const biggest = rows.filter((transaction) => transaction.type === 'Despesa').sort((a, b) => Number(b.value) - Number(a.value)).slice(0, 10);

  return (
    <div className="space-y-6">
      <div className="flex flex-col justify-between gap-3 md:flex-row md:items-end">
        <div>
          <h1 className="text-3xl font-black">Resumo mensal</h1>
          <p className="text-slate-600 dark:text-slate-300">Entradas, saídas, saldo, maiores gastos e categorias do mês selecionado.</p>
        </div>
        <div className="flex gap-2">
          <input className="input" type="month" value={month} onChange={(event) => setMonth(event.target.value)} />
          <button className="btn btn-primary" onClick={() => exportMonthlyReportXlsx(rows, month)}>Exportar Excel mensal</button>
        </div>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Entradas" value={formatCurrency(income)} icon={ArrowUpCircle} tone="green" />
        <StatCard title="Saídas" value={formatCurrency(expense)} icon={ArrowDownCircle} tone="red" />
        <StatCard title="Saldo do mês" value={formatCurrency(net)} icon={Wallet} tone={Number(net) >= 0 ? 'green' : 'red'} />
        <StatCard title="Maior gasto" value={formatCurrency(biggest[0]?.value ?? 0)} icon={Crown} tone="amber" hint={biggest[0]?.description ?? 'Sem despesas'} />
      </div>
      <div className="grid gap-6 xl:grid-cols-2">
        <ChartCard title="Categorias do mês" subtitle="Distribuição das saídas pagas">
          <div className="h-80">
            <ResponsiveContainer>
              <PieChart>
                <Pie data={categories} dataKey="value" nameKey="category" innerRadius={60} outerRadius={110} paddingAngle={4}>
                  {categories.map((_, index) => <Cell key={index} fill={['#14b8a6', '#f97316', '#6366f1', '#ef4444', '#8b5cf6', '#22c55e'][index % 6]} />)}
                </Pie>
                <Tooltip formatter={(value) => formatCurrency(Number(value))} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </ChartCard>
        <section className="card p-5">
          <h3 className="text-lg font-black">Maiores gastos</h3>
          <div className="mt-4 space-y-2">
            {biggest.map((transaction) => (
              <div className="flex items-center justify-between rounded-2xl bg-slate-50 p-3 dark:bg-slate-900" key={transaction.id}>
                <div><b>{transaction.description}</b><p className="text-xs text-slate-600 dark:text-slate-300">{transaction.category}</p></div>
                <b>{formatCurrency(transaction.value)}</b>
              </div>
            ))}
          </div>
        </section>
      </div>
      <DataTable rows={rows} />
    </div>
  );
}
