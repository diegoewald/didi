import * as XLSX from 'xlsx';
import type { BackupPayload, Transaction } from '../../types';
import { spreadsheetColumns } from '../../constants/defaults';
import { downloadBlob, safeFileDate } from '../formatters/formatters';
import { backupSchema } from '../validation/schemas';

export function exportBackup(payload: BackupPayload): void { downloadBlob(new Blob([JSON.stringify(payload, null, 2)], { type: 'application/json' }), `backup-financaspro-${safeFileDate()}.json`); }
export async function parseBackup(file: File): Promise<BackupPayload> { const json = JSON.parse(await file.text()) as unknown; return backupSchema.parse(json) as BackupPayload; }
export function exportTransactionsXlsx(transactions: Transaction[]): void { const rows = transactions.map(t => ({ Data: t.date, Descrição: t.description, Tipo: t.type, Categoria: t.category, Subcategoria: t.subcategory ?? '', Valor: t.value, Conta: t.account, 'Forma de pagamento': t.paymentMethod, Status: t.status, Parcela: t.installment, 'Total de parcelas': t.totalInstallments, Vencimento: t.dueDate ?? '', 'Pago em': t.paidAt ?? '', Observações: t.notes ?? '', 'ID externo': t.externalId ?? '' })); const ws = XLSX.utils.json_to_sheet(rows, { header: [...spreadsheetColumns] }); const wb = XLSX.utils.book_new(); XLSX.utils.book_append_sheet(wb, ws, 'Lançamentos'); XLSX.writeFile(wb, `lancamentos-financaspro-${safeFileDate()}.xlsx`); }
export function exportTransactionsCsv(transactions: Transaction[]): void { const ws = XLSX.utils.json_to_sheet(transactions); const csv = XLSX.utils.sheet_to_csv(ws); downloadBlob(new Blob([csv], { type: 'text/csv;charset=utf-8' }), `lancamentos-financaspro-${safeFileDate()}.csv`); }
