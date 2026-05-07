import { AlertTriangle, BadgeDollarSign, Banknote, PiggyBank, TrendingDown, TrendingUp, Wallet } from 'lucide-react';
import { CartesianGrid, Cell, Line, LineChart, Pie, PieChart, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';
import { ChartCard } from '../../components/ui/ChartCard';
import { StatCard } from '../../components/ui/StatCard';
import { categoryExpensePercentages, financialSummary, monthlyEvolution, overdueBills, pendingBills } from '../../lib/calculations/finance';
import { formatCurrency, formatDateBR } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

export function DashboardPage() {
  const { transactions, budgets } = useFinanceStore();
  const summary = financialSummary(transactions);
  const evolution = monthlyEvolution(transactions).map((month) => ({
    ...month,
    income: Number(month.income),
    expense: Number(month.expense),
    net: Number(month.net),
  }));
  const categories = categoryExpensePercentages(transactions);
  const pending = pendingBills(transactions).slice(0, 5);
  const overdue = overdueBills(transactions);
  const top = transactions.filter((transaction) => transaction.type === 'Despesa').sort((a, b) => Number(b.value) - Number(a.value)).slice(0, 10);
  const previous = Number(summary.previousMonthNet);
  const current = Number(summary.monthlyNet);
  const trend = previous === 0 ? 0 : ((current - previous) / Math.abs(previous)) * 100;
  const alerts = [
    ...(overdue.length ? [`${overdue.length} conta(s) atrasada(s) precisam de atenção.`] : []),
    ...(budgets.length ? [] : ['Defina orçamentos mensais por categoria para receber alertas de 80% e 100%.']),
    ...(summary.savingsRate < 10 ? ['Taxa de economia abaixo de 10%; revise despesas variáveis.'] : ['Boa taxa de economia neste mês.']),
  ];

  return (
    <div className="space-y-7">
      <div className="card relative overflow-hidden p-6 sm:p-7">
        <div className="absolute -right-16 -top-20 h-44 w-44 rounded-full bg-teal-200/40 blur-3xl dark:bg-teal-500/10" />
        <div className="relative flex flex-col justify-between gap-4 md:flex-row md:items-end">
          <div>
            <p className="text-xs font-black uppercase tracking-[.24em] text-teal-700 dark:text-teal-300">Painel executivo</p>
            <h1 className="mt-2 text-3xl font-black tracking-[-.05em] text-slate-950 dark:text-white sm:text-4xl">Dashboard financeiro</h1>
            <p className="mt-2 max-w-2xl text-sm font-medium leading-6 text-slate-500 dark:text-slate-400">Visão executiva com caixa, competência, saúde financeira e alertas inteligentes.</p>
          </div>
          <div className="rounded-3xl border border-emerald-200 bg-emerald-50 px-5 py-3 text-sm font-black text-emerald-800 shadow-sm dark:border-emerald-900 dark:bg-emerald-950/70 dark:text-emerald-100">
            Saúde financeira: {summary.healthScore}/100
          </div>
        </div>
      </div>
      <div className="grid gap-4 md:grid-cols-2 xl:grid-cols-4">
        <StatCard title="Saldo atual" value={formatCurrency(summary.balance)} icon={Wallet} tone="blue" hint="Somente lançamentos pagos afetam o caixa real" />
        <StatCard title="Receitas do mês" value={formatCurrency(summary.monthlyIncome)} icon={TrendingUp} tone="green" />
        <StatCard title="Despesas do mês" value={formatCurrency(summary.monthlyExpense)} icon={TrendingDown} tone="red" />
        <StatCard title="Resultado líquido" value={formatCurrency(summary.monthlyNet)} icon={PiggyBank} tone={current >= 0 ? 'green' : 'red'} trend={trend} />
      </div>
      <div className="grid gap-6 xl:grid-cols-3">
        <ChartCard title="Evolução mensal" subtitle="Receitas, despesas e resultado líquido">
          <div className="h-80">
            <ResponsiveContainer>
              <LineChart data={evolution} margin={{ left: 4, right: 12, top: 8, bottom: 0 }}>
                <CartesianGrid strokeDasharray="4 4" opacity={0.18} />
                <XAxis dataKey="month" tickLine={false} axisLine={false} />
                <YAxis tickLine={false} axisLine={false} tickFormatter={(value) => `R$${Number(value) / 1000}k`} />
                <Tooltip formatter={(value) => formatCurrency(Number(value))} contentStyle={{ borderRadius: 18, border: '1px solid rgba(148,163,184,.25)' }} />
                <Line type="monotone" dataKey="income" stroke="#10b981" strokeWidth={3.4} dot={false} name="Receitas" />
                <Line type="monotone" dataKey="expense" stroke="#ef4444" strokeWidth={3.4} dot={false} name="Despesas" />
                <Line type="monotone" dataKey="net" stroke="#6366f1" strokeWidth={3.4} dot={false} name="Resultado" />
              </LineChart>
            </ResponsiveContainer>
          </div>
        </ChartCard>
        <ChartCard title="Gastos por categoria" subtitle="Distribuição de despesas">
          <div className="h-80">
            <ResponsiveContainer>
              <PieChart>
                <Pie data={categories} dataKey="value" nameKey="category" innerRadius={68} outerRadius={108} paddingAngle={5}>
                  {categories.map((_, index) => <Cell key={index} fill={['#14b8a6', '#f97316', '#6366f1', '#ef4444', '#8b5cf6', '#22c55e'][index % 6]} />)}
                </Pie>
                <Tooltip formatter={(value) => formatCurrency(Number(value))} contentStyle={{ borderRadius: 18, border: '1px solid rgba(148,163,184,.25)' }} />
              </PieChart>
            </ResponsiveContainer>
          </div>
        </ChartCard>
        <section className="card p-5 sm:p-6">
          <h3 className="text-lg font-black tracking-[-.03em]">Alertas inteligentes</h3>
          <div className="mt-4 space-y-3">
            {alerts.map((alert) => (
              <div key={alert} className="flex gap-3 rounded-2xl border border-amber-200/70 bg-amber-50/90 p-3.5 text-sm font-bold text-amber-900 shadow-sm dark:border-amber-900/70 dark:bg-amber-950/45 dark:text-amber-100">
                <AlertTriangle className="shrink-0" size={18} />
                <span>{alert}</span>
              </div>
            ))}
          </div>
          <div className="mt-5 grid grid-cols-2 gap-3">
            <div className="rounded-2xl bg-slate-100/80 p-3 dark:bg-slate-900/80"><p className="text-xs font-bold text-slate-500">Taxa de economia</p><b className="text-xl">{summary.savingsRate.toFixed(1)}%</b></div>
            <div className="rounded-2xl bg-slate-100/80 p-3 dark:bg-slate-900/80"><p className="text-xs font-bold text-slate-500">Mês atual x anterior</p><b className="text-xl">{trend.toFixed(1)}%</b></div>
          </div>
        </section>
      </div>
      <div className="grid gap-6 xl:grid-cols-2">
        <section className="card p-5 sm:p-6">
          <h3 className="mb-4 flex items-center gap-2 text-lg font-black tracking-[-.03em]"><BadgeDollarSign className="text-teal-600" />Top 10 maiores despesas</h3>
          <div className="space-y-2.5">
            {top.map((transaction) => <div key={transaction.id} className="flex items-center justify-between gap-4 rounded-2xl bg-slate-50/90 p-3.5 transition hover:bg-teal-50 dark:bg-slate-900/70 dark:hover:bg-slate-900"><div className="min-w-0"><b className="block truncate">{transaction.description}</b><p className="text-xs font-medium text-slate-500">{transaction.category} • {formatDateBR(transaction.date)}</p></div><b className="shrink-0">{formatCurrency(transaction.value)}</b></div>)}
          </div>
        </section>
        <section className="card p-5 sm:p-6">
          <h3 className="mb-4 flex items-center gap-2 text-lg font-black tracking-[-.03em]"><Banknote className="text-teal-600" />Próximas contas a vencer</h3>
          <div className="space-y-2.5">
            {pending.map((transaction) => <div key={transaction.id} className="flex items-center justify-between gap-4 rounded-2xl bg-slate-50/90 p-3.5 transition hover:bg-teal-50 dark:bg-slate-900/70 dark:hover:bg-slate-900"><div className="min-w-0"><b className="block truncate">{transaction.description}</b><p className="text-xs font-medium text-slate-500">Vence em {formatDateBR(transaction.dueDate)}</p></div><b className="shrink-0">{formatCurrency(transaction.value)}</b></div>)}
          </div>
        </section>
      </div>
    </div>
  );
}
