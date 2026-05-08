import type { Transaction } from '../../types';
import { formatCurrency, formatDateBR } from '../../lib/formatters/formatters';

export function ImportPreviewTable({ rows }: { rows: Transaction[] }) {
  return (
    <div className="overflow-auto rounded-[1.4rem] border border-slate-200 bg-white/90 shadow-sm dark:border-slate-800 dark:bg-slate-950/90 scrollbar">
      <table className="w-full min-w-[760px] text-sm">
        <thead className="bg-slate-50 text-left text-xs uppercase tracking-[.11em] text-slate-600 dark:text-slate-300 dark:bg-slate-900 dark:text-slate-400">
          <tr><th className="p-4">Data</th><th>Descrição</th><th>Tipo</th><th>Categoria</th><th>Valor</th><th>Status</th></tr>
        </thead>
        <tbody className="divide-y divide-slate-100 dark:divide-slate-800">
          {rows.slice(0, 50).map((row) => <tr key={row.id} className="transition hover:bg-teal-50/50 dark:hover:bg-slate-900"><td className="p-4 font-bold">{formatDateBR(row.date)}</td><td className="font-semibold">{row.description}</td><td>{row.type}</td><td>{row.category}</td><td className="font-black">{formatCurrency(row.value)}</td><td><span className="badge dark:bg-slate-900 dark:text-slate-200">{row.status}</span></td></tr>)}
        </tbody>
      </table>
    </div>
  );
}
