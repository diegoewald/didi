import { CheckCircle2, Copy, Pencil, Trash2 } from 'lucide-react';
import type { Transaction } from '../../types';
import { formatCurrency, formatDateBR } from '../../lib/formatters/formatters';

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
    <div className="overflow-hidden rounded-3xl border border-slate-200 dark:border-slate-800">
      <div className="max-h-[560px] overflow-auto scrollbar">
        <table className="w-full min-w-[1080px] border-collapse text-sm">
          <thead className="sticky top-0 bg-slate-100 text-left text-xs uppercase tracking-wide text-slate-500 dark:bg-slate-900">
            <tr>{['Data', 'Descrição', 'Tipo', 'Categoria', 'Valor', 'Conta', 'Status', 'Pagamento', 'Ações'].map((header) => <th key={header} className="px-4 py-3">{header}</th>)}</tr>
          </thead>
          <tbody>
            {rows.map((transaction) => (
              <tr key={transaction.id} className="border-t border-slate-100 bg-white hover:bg-slate-50 dark:border-slate-800 dark:bg-slate-950 dark:hover:bg-slate-900">
                <td className="px-4 py-3 font-bold">{formatDateBR(transaction.date)}</td>
                <td className="px-4 py-3"><b>{transaction.description}</b><p className="text-xs text-slate-500">{transaction.notes}</p></td>
                <td className="px-4 py-3"><span className={`badge ${transaction.type === 'Receita' ? 'bg-emerald-100 text-emerald-700' : transaction.type === 'Despesa' ? 'bg-rose-100 text-rose-700' : 'bg-blue-100 text-blue-700'}`}>{transaction.type}</span></td>
                <td className="px-4 py-3">{transaction.category}<p className="text-xs text-slate-500">{transaction.subcategory}</p></td>
                <td className="px-4 py-3 font-black">{formatCurrency(transaction.value)}</td>
                <td className="px-4 py-3">{transaction.account}</td>
                <td className="px-4 py-3"><span className="badge bg-slate-100 text-slate-700 dark:bg-slate-800 dark:text-slate-200">{transaction.status}</span></td>
                <td className="px-4 py-3">{transaction.paymentMethod}</td>
                <td className="px-4 py-3">
                  <div className="flex flex-wrap gap-2">
                    <button className="btn btn-secondary !p-2" title="Editar" aria-label="Editar lançamento" onClick={() => onEdit?.(transaction)}><Pencil size={15} /></button>
                    <button className="btn btn-secondary !p-2" title="Duplicar" aria-label="Duplicar lançamento" onClick={() => onDuplicate?.(transaction)}><Copy size={15} /></button>
                    {transaction.status !== 'Pago' && <button className="btn btn-secondary !p-2" title="Marcar como pago" aria-label="Marcar lançamento como pago" onClick={() => onMarkPaid?.(transaction)}><CheckCircle2 size={15} /></button>}
                    <button className="btn bg-rose-100 !p-2 text-rose-700" title="Excluir" aria-label="Excluir lançamento" onClick={() => onDelete?.(transaction)}><Trash2 size={15} /></button>
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
