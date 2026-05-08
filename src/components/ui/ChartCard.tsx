import type { ReactNode } from 'react';

export function ChartCard({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) {
  return (
    <section className="card overflow-hidden p-5 sm:p-6">
      <div className="mb-5 flex items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-black tracking-[-.03em] text-slate-950 dark:text-white sm:text-xl">{title}</h3>
          {subtitle && <p className="mt-1 text-sm font-medium text-slate-600 dark:text-slate-300 dark:text-slate-400">{subtitle}</p>}
        </div>
        <div className="h-2 w-16 rounded-full bg-gradient-to-r from-teal-400 to-indigo-500" />
      </div>
      {children}
    </section>
  );
}
