import { spreadsheetColumns } from '../../constants/defaults';
import type { ColumnMapping } from '../../lib/spreadsheet/importer';

export function ColumnMapper({ headers, mapping, onChange }: { headers: string[]; mapping: ColumnMapping; onChange: (m: ColumnMapping) => void }) {
  return (
    <div className="grid gap-3 md:grid-cols-3">
      {spreadsheetColumns.map((col) => (
        <label key={col} className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
          <span className="label">{col}</span>
          <select className="input" value={mapping[col] ?? ''} onChange={(event) => onChange({ ...mapping, [col]: event.target.value || undefined })}>
            <option value="">Não mapear</option>
            {headers.map((header) => <option key={header} value={header}>{header}</option>)}
          </select>
        </label>
      ))}
    </div>
  );
}
