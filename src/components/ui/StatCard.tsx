import type { LucideIcon } from 'lucide-react';
import { ArrowDownRight, ArrowUpRight } from 'lucide-react';

interface Props {
  title: string;
  value: string;
  hint?: string;
  icon: LucideIcon;
  trend?: number;
  tone?: 'green' | 'red' | 'blue' | 'amber' | 'slate';
}

export function StatCard({ title, value, hint, icon: Icon, trend, tone = 'blue' }: Props) {
  const map = {
    green: 'from-emerald-500 via-teal-500 to-cyan-500 shadow-emerald-500/20',
    red: 'from-rose-500 via-orange-500 to-amber-500 shadow-rose-500/20',
    blue: 'from-blue-500 via-indigo-500 to-violet-500 shadow-blue-500/20',
    amber: 'from-amber-400 via-orange-500 to-rose-500 shadow-amber-500/20',
    slate: 'from-slate-700 via-slate-800 to-slate-950 shadow-slate-500/20',
  };
  return (
    <section className="card animate-rise group relative overflow-hidden p-5 sm:p-6">
      <div className="absolute -right-12 -top-14 h-32 w-32 rounded-full bg-teal-200/24 blur-2xl transition group-hover:scale-110 dark:bg-teal-500/10" />
      <div className="relative flex items-start justify-between gap-4">
        <div className="min-w-0">
          <p className="text-xs font-black uppercase tracking-[.16em] text-slate-500 dark:text-slate-400">{title}</p>
          <h3 className="mt-2 truncate text-2xl font-black tracking-[-.04em] text-slate-950 dark:text-white sm:text-3xl">{value}</h3>
          {hint && <p className="mt-2 line-clamp-2 text-xs font-semibold text-slate-500 dark:text-slate-400">{hint}</p>}
        </div>
        <div className={`rounded-3xl bg-gradient-to-br ${map[tone]} p-3.5 text-white shadow-2xl transition group-hover:-translate-y-0.5 group-hover:scale-105`}>
          <Icon size={23} />
        </div>
      </div>
      {trend !== undefined && (
        <div className="relative mt-5 inline-flex items-center gap-2 rounded-full bg-slate-100 px-3 py-2 text-xs font-black text-slate-600 dark:bg-slate-900 dark:text-slate-300">
          <span className={trend >= 0 ? 'text-emerald-600' : 'text-rose-600'}>
            {trend >= 0 ? <ArrowUpRight size={16} /> : <ArrowDownRight size={16} />}
          </span>
          {Math.abs(trend).toFixed(1)}% vs. mês anterior
        </div>
      )}
    </section>
  );
}
