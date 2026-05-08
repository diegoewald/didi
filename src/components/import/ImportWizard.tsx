import { useState } from 'react';
import * as XLSX from 'xlsx';
import { Download, FileSpreadsheet, RotateCcw, ShieldCheck } from 'lucide-react';
import { buildTemplateWorkbook, detectColumnMapping, readSpreadsheet, validateRows, type ColumnMapping } from '../../lib/spreadsheet/importer';
import type { ImportResult, SpreadsheetRow, Transaction } from '../../types';
import { ColumnMapper } from './ColumnMapper';
import { ImportPreviewTable } from './ImportPreviewTable';

export function ImportWizard({ existing, onConfirm }: { existing: Transaction[]; onConfirm: (rows: Transaction[]) => Promise<number> }) {
  const [step, setStep] = useState(1);
  const [headers, setHeaders] = useState<string[]>([]);
  const [rows, setRows] = useState<SpreadsheetRow[]>([]);
  const [mapping, setMapping] = useState<ColumnMapping>({});
  const [result, setResult] = useState<ImportResult>();
  const [message, setMessage] = useState('');
  const [completed, setCompleted] = useState(false);
  const [importing, setImporting] = useState(false);

  const reset = () => {
    setStep(1);
    setHeaders([]);
    setRows([]);
    setMapping({});
    setResult(undefined);
    setMessage('');
    setCompleted(false);
    setImporting(false);
  };

  const upload = async (file?: File) => {
    if (!file || importing) return;
    if (!/\.(xlsx|csv)$/i.test(file.name)) {
      setMessage('Arquivo inválido. Envie .xlsx ou .csv.');
      return;
    }
    try {
      setMessage('Lendo planilha localmente...');
      const data = await readSpreadsheet(file);
      setRows(data.rows);
      setHeaders(data.headers);
      setMapping(detectColumnMapping(data.headers));
      setResult(undefined);
      setCompleted(false);
      setStep(3);
      setMessage(`${data.rows.length} linhas lidas. Nenhum dado foi enviado para servidores.`);
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Erro inesperado ao ler arquivo.');
    }
  };

  const validate = () => {
    if (completed || importing) return;
    const nextResult = validateRows(rows, mapping, existing);
    setResult(nextResult);
    setStep(5);
    setMessage(`${nextResult.validRows.length} válidas, ${nextResult.errors.length} erro(s), ${nextResult.duplicates.length} duplicada(s), ${nextResult.ignoredRows} vazia(s).`);
  };

  const confirm = async () => {
    if (completed || importing) return;
    if (!result?.validRows.length) {
      setMessage('Nenhuma linha válida foi importada. Revise os erros e tente novamente.');
      return;
    }
    try {
      setImporting(true);
      const imported = await onConfirm(result.validRows);
      setCompleted(true);
      setStep(7);
      setRows([]);
      setHeaders([]);
      setMapping({});
      setResult(undefined);
      setMessage(imported > 0 ? `Importação concluída com sucesso. ${imported} lançamentos foram importados.` : 'Nenhuma linha nova foi importada. Os lançamentos desta prévia já existiam.');
    } catch (error) {
      setMessage(error instanceof Error ? `Falha ao confirmar importação: ${error.message}` : 'Falha ao confirmar importação. Tente novamente.');
    } finally {
      setImporting(false);
    }
  };

  const template = () => { XLSX.writeFile(buildTemplateWorkbook(), 'modelo-financaspro.xlsx'); };
  const steps = ['Upload', 'Leitura', 'Mapeamento', 'Validação', 'Prévia', 'Confirmação', 'Resultado'];

  return (
    <section className="card space-y-6 overflow-hidden p-5 sm:p-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-start">
        <div>
          <p className="text-xs font-black uppercase tracking-[.2em] text-teal-700 dark:text-teal-300">Importação assistida</p>
          <h2 className="mt-1 text-2xl font-black tracking-[-.04em] sm:text-3xl">Import Wizard</h2>
          <p className="mt-2 max-w-3xl text-sm font-medium leading-6 text-slate-600 dark:text-slate-300">Upload → leitura → mapeamento → validação → prévia → confirmação → resultado. Também aceita extratos comuns com Valor negativo ou colunas Crédito/Débito.</p>
        </div>
        <button className="btn btn-secondary" onClick={template}><Download size={18} />Baixar modelo de planilha</button>
      </div>

      <div className="grid gap-2 sm:grid-cols-2 md:grid-cols-4 xl:grid-cols-7">
        {steps.map((stepName, index) => {
          const active = step >= index + 1;
          return (
            <div key={stepName} className={`rounded-2xl border p-3 text-center text-xs font-black transition ${active ? 'border-teal-300 bg-gradient-to-br from-teal-600 to-cyan-600 text-white shadow-lg shadow-teal-500/20' : 'border-slate-300 bg-slate-100 text-slate-600 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-300'}`}>
              <span className="mb-1 block text-[.68rem] opacity-80">Etapa {index + 1}</span>{stepName}
            </div>
          );
        })}
      </div>

      {message && <div className="flex items-start gap-3 rounded-3xl p-4 text-sm font-bold shadow-sm alert-info"><ShieldCheck className="shrink-0" size={18} />{message}</div>}

      {step <= 2 && !completed && (
        <label className="group grid cursor-pointer place-items-center rounded-[2rem] border-2 border-dashed border-teal-400 bg-gradient-to-br from-white to-teal-50 p-8 text-center transition hover:-translate-y-0.5 hover:border-teal-500 hover:shadow-xl dark:border-teal-700 dark:from-slate-950 dark:to-teal-950/30 sm:p-12">
          <div className="grid h-20 w-20 place-items-center rounded-3xl bg-white text-teal-700 shadow-xl transition group-hover:scale-105 dark:bg-slate-900 dark:text-teal-300"><FileSpreadsheet size={44} /></div>
          <b className="mt-5 text-lg tracking-[-.02em]">Clique para selecionar .xlsx ou .csv</b>
          <span className="mt-2 max-w-2xl text-sm font-medium leading-6 text-slate-600 dark:text-slate-300">A leitura acontece apenas no seu navegador. Extratos com Data, Histórico, Valor, Saldo, Crédito ou Débito são reconhecidos.</span>
          <input type="file" accept=".xlsx,.csv" hidden onChange={(event) => { void upload(event.target.files?.[0]); event.currentTarget.value = ''; }} />
        </label>
      )}

      {step >= 3 && step < 5 && !completed && (
        <>
          <ColumnMapper headers={headers} mapping={mapping} onChange={setMapping} />
          <button className="btn btn-primary self-start" onClick={validate} disabled={importing}>Validar dados e gerar prévia</button>
        </>
      )}

      {step >= 5 && result && !completed && (
        <div className="space-y-5">
          <div className="grid gap-3 md:grid-cols-4"><Metric label="Importar" value={result.validRows.length} /><Metric label="Erros" value={result.errors.length} /><Metric label="Duplicados" value={result.duplicates.length} /><Metric label="Ignorados" value={result.ignoredRows} /></div>
          {result.errors.concat(result.duplicates).length > 0 && <div className="max-h-52 overflow-auto rounded-3xl p-4 text-sm font-semibold scrollbar alert-warning">{result.errors.concat(result.duplicates).map((error, index) => <p key={index}><b>Linha {error.row}</b> {error.field}: {error.message}</p>)}</div>}
          <ImportPreviewTable rows={result.validRows} />
          <button className="btn btn-primary" disabled={!result.validRows.length || importing || completed} onClick={() => { void confirm(); }}>{importing ? 'Importando...' : 'Confirmar importação'}</button>
        </div>
      )}

      {completed && (
        <div className="flex flex-wrap items-center gap-3 rounded-3xl p-4 alert-success">
          <ShieldCheck size={20} />
          <b>Prévia encerrada para evitar reimportação acidental.</b>
          <button className="btn btn-secondary" onClick={reset}><RotateCcw size={17} />Importar outra planilha</button>
        </div>
      )}
    </section>
  );
}

function Metric({ label, value }: { label: string; value: number }) {
  return <div className="rounded-3xl border border-slate-300 bg-slate-50 p-4 shadow-sm dark:border-slate-700 dark:bg-slate-900"><p className="text-xs font-black uppercase tracking-[.12em] text-slate-600 dark:text-slate-300">{label}</p><b className="mt-1 block text-3xl tracking-[-.04em]">{value}</b></div>;
}
