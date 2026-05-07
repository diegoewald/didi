import * as XLSX from 'xlsx';
import type { BackupPayload, EncryptedBackupEnvelope, Transaction } from '../../types';
import { spreadsheetColumns } from '../../constants/defaults';
import { downloadBlob, safeFileDate } from '../formatters/formatters';
import { backupSchema } from '../validation/schemas';
import { encryptBackup, decryptBackup } from './cryptoBackup';

export function exportBackup(payload: BackupPayload): void {
  downloadBlob(
    new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' }),
    `backup-financaspro-${safeFileDate()}.json`,
  );
}

export async function exportEncryptedBackup(
  payload: BackupPayload,
  password: string,
): Promise<void> {
  const encrypted = await encryptBackup(payload, password);
  downloadBlob(
    new Blob([JSON.stringify(encrypted, null, 2)], { type: 'application/json' }),
    `backup-financaspro-criptografado-${safeFileDate()}.json`,
  );
}

export async function parseBackup(file: File, password?: string): Promise<BackupPayload> {
  const json = JSON.parse(await file.text()) as unknown;
  if (isEncryptedEnvelope(json)) {
    if (!password) throw new Error('Informe a senha para restaurar este backup criptografado.');
    const decrypted = await decryptBackup(json, password);
    return backupSchema.parse(decrypted) as BackupPayload;
  }
  return backupSchema.parse(json) as BackupPayload;
}

function isEncryptedEnvelope(value: unknown): value is EncryptedBackupEnvelope {
  return Boolean(
    value &&
      typeof value === 'object' &&
      'encrypted' in value &&
      (value as { encrypted?: unknown }).encrypted === true,
  );
}

function transactionToSheetRow(t: Transaction) {
  return {
    Data: t.date,
    Descrição: t.description,
    Tipo: t.type,
    Categoria: t.category,
    Subcategoria: t.subcategory ?? '',
    Valor: t.value,
    Conta: t.account,
    'Forma de pagamento': t.paymentMethod,
    Status: t.status,
    Parcela: t.installment,
    'Total de parcelas': t.totalInstallments,
    Vencimento: t.dueDate ?? '',
    'Pago em': t.paidAt ?? '',
    Observações: t.notes ?? '',
    'ID externo': t.externalId ?? '',
  };
}

export function exportTransactionsXlsx(transactions: Transaction[]): void {
  const rows = transactions.map(transactionToSheetRow);
  const ws = XLSX.utils.json_to_sheet(rows, { header: [...spreadsheetColumns] });
  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(wb, ws, 'Lançamentos');
  XLSX.writeFile(wb, `lancamentos-financaspro-${safeFileDate()}.xlsx`);
}

export function exportMonthlyReportXlsx(transactions: Transaction[], month: string): void {
  const monthlyRows = transactions.filter((transaction) => transaction.date.startsWith(month));
  const income = monthlyRows
    .filter((transaction) => transaction.type === 'Receita')
    .reduce((total, transaction) => total + Number(transaction.value), 0);
  const expense = monthlyRows
    .filter((transaction) => transaction.type === 'Despesa')
    .reduce((total, transaction) => total + Number(transaction.value), 0);
  const byCategory = new Map<string, number>();
  monthlyRows
    .filter((transaction) => transaction.type === 'Despesa')
    .forEach((transaction) => {
      byCategory.set(transaction.category, (byCategory.get(transaction.category) ?? 0) + Number(transaction.value));
    });

  const wb = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(
    wb,
    XLSX.utils.json_to_sheet([
      { Indicador: 'Entradas', Valor: income.toFixed(2) },
      { Indicador: 'Saídas', Valor: expense.toFixed(2) },
      { Indicador: 'Saldo', Valor: (income - expense).toFixed(2) },
      { Indicador: 'Quantidade de lançamentos', Valor: monthlyRows.length },
    ]),
    'Resumo',
  );
  XLSX.utils.book_append_sheet(wb, XLSX.utils.json_to_sheet(monthlyRows.map(transactionToSheetRow)), 'Lançamentos');
  XLSX.utils.book_append_sheet(
    wb,
    XLSX.utils.json_to_sheet(
      [...byCategory.entries()].map(([Categoria, Valor]) => ({ Categoria, Valor: Valor.toFixed(2) })),
    ),
    'Categorias',
  );
  XLSX.writeFile(wb, `relatorio-mensal-financaspro-${month}-${safeFileDate()}.xlsx`);
}

export function exportTransactionsCsv(transactions: Transaction[]): void {
  const ws = XLSX.utils.json_to_sheet(transactions.map(transactionToSheetRow));
  const csv = XLSX.utils.sheet_to_csv(ws);
  downloadBlob(
    new Blob([csv], { type: 'text/csv;charset=utf-8' }),
    `lancamentos-financaspro-${safeFileDate()}.csv`,
  );
}
