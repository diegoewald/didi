import { CheckCircle2, Copy, Pencil, Trash2 } from 'lucide-react';
import type { Transaction, TransactionStatus } from '../../types';
import { formatCurrency, formatDateBR } from '../../lib/formatters/formatters';

function statusClass(status: TransactionStatus): string {
  const classes: Record<TransactionStatus, string> = {
    Pago: 'bg-emerald-50 text-emerald-700 ring-emerald-200 dark:bg-emerald-950/50 dark:text-emerald-200 dark:ring-emerald-800',
    Pendente: 'bg-amber-50 text-amber-700 ring-amber-200 dark:bg-amber-950/50 dark:text-amber-200 dark:ring-amber-800',
    Atrasado: 'bg-rose-50 text-rose-700 ring-rose-200 dark:bg-rose-950/50 dark:text-rose-200 dark:ring-rose-800',
    Cancelado: 'bg-slate-100 text-slate-600 ring-slate-200 dark:bg-slate-900 dark:text-slate-300 dark:ring-slate-700',
  };
  return classes[status];
}

export function DataTable({
  rows,
  onEdit,
  onDelete,
  onDuplicate,
  onMarkPaid,
}: {
  rows: Transaction[];
  onEdit?: (transaction: Transaction) => void;
  onDelete?: (transaction: Transaction) => void;
  onDuplicate?: (transaction: Transaction) => void;
  onMarkPaid?: (transaction: Transaction) => void;
}) {
  return (
    <div className="overflow-hidden rounded-[1.4rem] border border-slate-200/80 bg-white/72 shadow-sm dark:border-slate-800/80 dark:bg-slate-950/50">
      <div className="overflow-auto scrollbar">
        <table className="w-full min-w-[1040px] border-collapse text-sm">
          <thead>
            <tr className="bg-slate-50/95 text-left text-[.72rem] uppercase tracking-[.11em] text-slate-500 dark:bg-slate-900/95 dark:text-slate-400">
              {['Data', 'Descrição', 'Tipo', 'Categoria', 'Valor', 'Conta', 'Status', 'Pagamento', 'Ações'].map((header) => (
                <th key={header} className="px-5 py-4 font-black first:rounded-tl-[1.35rem] last:rounded-tr-[1.35rem]">{header}</th>
              ))}
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 dark:divide-slate-800/80">
            {rows.map((transaction) => (
              <tr key={transaction.id} className="group bg-white/80 transition hover:bg-teal-50/45 dark:bg-slate-950/35 dark:hover:bg-slate-900/80">
                <td className="px-5 py-4 font-black text-slate-700 dark:text-slate-200">{formatDateBR(transaction.date)}</td>
                <td className="max-w-[260px] px-5 py-4">
                  <b className="block truncate text-slate-950 dark:text-white">{transaction.description}</b>
                  <p className="mt-1 truncate text-xs font-medium text-slate-500 dark:text-slate-400">{transaction.notes}</p>
                </td>
                <td className="px-5 py-4">
                  <span className={`badge ${transaction.type === 'Receita' ? 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950/50 dark:text-emerald-200' : transaction.type === 'Despesa' ? 'bg-rose-50 text-rose-700 dark:bg-rose-950/50 dark:text-rose-200' : 'bg-blue-50 text-blue-700 dark:bg-blue-950/50 dark:text-blue-200'}`}>{transaction.type}</span>
                </td>
                <td className="px-5 py-4 font-semibold text-slate-700 dark:text-slate-200">
                  {transaction.category}
                  <p className="mt-1 text-xs font-medium text-slate-500 dark:text-slate-400">{transaction.subcategory}</p>
                </td>
                <td className="px-5 py-4 text-base font-black tracking-[-.02em] text-slate-950 dark:text-white">{formatCurrency(transaction.value)}</td>
                <td className="px-5 py-4 font-semibold text-slate-600 dark:text-slate-300">{transaction.account}</td>
                <td className="px-5 py-4"><span className={`badge ring-1 ${statusClass(transaction.status)}`}>{transaction.status}</span></td>
                <td className="px-5 py-4 font-semibold text-slate-600 dark:text-slate-300">{transaction.paymentMethod}</td>
                <td className="px-5 py-4">
                  <div className="flex flex-wrap gap-2 opacity-95 transition group-hover:opacity-100">
                    <button className="btn btn-secondary !min-h-0 !p-2.5" title="Editar" aria-label="Editar lançamento" onClick={() => onEdit?.(transaction)}><Pencil size={15} /></button>
                    <button className="btn btn-secondary !min-h-0 !p-2.5" title="Duplicar" aria-label="Duplicar lançamento" onClick={() => onDuplicate?.(transaction)}><Copy size={15} /></button>
                    {transaction.status !== 'Pago' && <button className="btn btn-secondary !min-h-0 !p-2.5" title="Marcar como pago" aria-label="Marcar lançamento como pago" onClick={() => onMarkPaid?.(transaction)}><CheckCircle2 size={15} /></button>}
                    <button className="btn !min-h-0 !p-2.5 bg-rose-50 text-rose-700 ring-1 ring-rose-100 hover:bg-rose-100 dark:bg-rose-950/40 dark:text-rose-200 dark:ring-rose-900" title="Excluir" aria-label="Excluir lançamento" onClick={() => onDelete?.(transaction)}><Trash2 size={15} /></button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
