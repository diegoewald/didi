import { spreadsheetColumns } from '../../constants/defaults';
import type { ColumnMapping } from '../../lib/spreadsheet/importer';

const bankStatementColumns = ['Crédito', 'Débito', 'Saldo'] as const;

export function ColumnMapper({ headers, mapping, onChange }: { headers: string[]; mapping: ColumnMapping; onChange: (m: ColumnMapping) => void }) {
  const renderSelect = (col: keyof ColumnMapping, optional = false) => (
    <label key={col} className="rounded-2xl border border-slate-200 bg-slate-50/80 p-3 dark:border-slate-800 dark:bg-slate-900/70">
      <span className="label">{col}{optional ? ' (opcional)' : ''}</span>
      <select className="input" value={mapping[col] ?? ''} onChange={(event) => onChange({ ...mapping, [col]: event.target.value || undefined })}>
        <option value="">Não mapear</option>
        {headers.map((header) => <option key={header} value={header}>{header}</option>)}
      </select>
    </label>
  );

  return (
    <div className="space-y-4">
      <div className="grid gap-3 md:grid-cols-3">
        {spreadsheetColumns.map((col) => renderSelect(col))}
      </div>
      <div className="rounded-3xl border border-teal-200 bg-teal-50/80 p-4 dark:border-teal-900 dark:bg-teal-950/30">
        <h3 className="text-sm font-black text-teal-900 dark:text-teal-100">Campos extras para extratos bancários</h3>
        <p className="mt-1 text-sm font-medium text-teal-800 dark:text-teal-200">Use Crédito/Débito quando o banco separa entradas e saídas. Saldo é aceito para conferência, mas não é importado como lançamento.</p>
        <div className="mt-3 grid gap-3 md:grid-cols-3">
          {bankStatementColumns.map((col) => renderSelect(col, true))}
        </div>
      </div>
    </div>
  );
}
