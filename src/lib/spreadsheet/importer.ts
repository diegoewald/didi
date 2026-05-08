import * as XLSX from 'xlsx';
import { spreadsheetColumns } from '../../constants/defaults';
import type { ImportError, ImportResult, SpreadsheetRow, Transaction, TransactionType } from '../../types';
import { parseCurrencyInput, parseDateInput, parseSignedCurrencyInput } from '../formatters/formatters';
import { paymentMethods, transactionSchema, transactionStatuses, transactionTypes } from '../validation/schemas';
import { createId } from '../utils/id';

export type ColumnKey = (typeof spreadsheetColumns)[number];
export type ColumnMapping = Partial<Record<ColumnKey | 'Crédito' | 'Débito' | 'Saldo', string>>;

const synonyms: Record<ColumnKey | 'Crédito' | 'Débito' | 'Saldo', string[]> = {
  Data: ['data', 'emissao', 'emissão', 'lancamento', 'lançamento', 'dt', 'data movimento', 'data transação'],
  Descrição: ['descricao', 'descrição', 'historico', 'histórico', 'detalhe', 'nome', 'memo'],
  Tipo: ['tipo', 'natureza', 'entrada/saida', 'entrada saída', 'debito/credito'],
  Categoria: ['categoria', 'grupo', 'classificacao', 'classificação'],
  Subcategoria: ['subcategoria', 'sub grupo', 'subgrupo'],
  Valor: ['valor', 'preco', 'preço', 'total', 'vlr', 'amount', 'valor lançamento'],
  Conta: ['conta', 'banco', 'carteira', 'account'],
  'Forma de pagamento': ['forma de pagamento', 'forma pagamento', 'pagamento', 'metodo', 'método', 'canal'],
  Status: ['status', 'situacao', 'situação'],
  Parcela: ['parcela', 'n parcela'],
  'Total de parcelas': ['total de parcelas', 'parcelas', 'qtd parcelas'],
  Vencimento: ['vencimento', 'data vencimento'],
  'Pago em': ['pago em', 'pagamento em', 'data pagamento', 'liquidação', 'liquidacao'],
  Observações: ['observacoes', 'observações', 'obs', 'nota', 'documento'],
  'ID externo': ['id externo', 'codigo', 'código', 'id', 'nsu', 'documento'],
  Crédito: ['credito', 'crédito', 'credit', 'entrada', 'receita'],
  Débito: ['debito', 'débito', 'debit', 'saida', 'saída', 'despesa'],
  Saldo: ['saldo', 'balance'],
};

const normalize = (value: string) =>
  value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase()
    .trim();

export function detectColumnMapping(headers: string[]): ColumnMapping {
  const mapping: ColumnMapping = {};
  for (const col of Object.keys(synonyms) as Array<keyof typeof synonyms>) {
    const candidates = [col, ...synonyms[col]].map(normalize);
    const found = headers.find((header) => candidates.includes(normalize(header)));
    if (found) mapping[col] = found;
  }
  return mapping;
}

export function suggestCategory(description: string, type: string): string {
  const normalized = normalize(description);
  if (type === 'Receita' && /salario|ordenado|pix recebido|ted recebida|credito salario/.test(normalized)) return 'Salário';
  if (/uber|99|combustivel|metro|onibus|posto|estacionamento/.test(normalized)) return 'Transporte';
  if (/mercado|supermercado|padaria|acougue|hortifruti/.test(normalized)) return 'Alimentação';
  if (/farmacia|medico|hospital|laboratorio|plano de saude/.test(normalized)) return 'Saúde';
  if (/netflix|spotify|prime|disney|assinatura|apple\.com|google/.test(normalized)) return 'Assinaturas';
  if (/aluguel|condominio|energia|internet|agua|luz|gas/.test(normalized)) return 'Contas da casa';
  if (/rendimento|dividendo|tesouro|investimento|corretora/.test(normalized)) return 'Investimentos';
  return type === 'Receita' ? 'Renda extra' : 'Outros';
}

export async function readSpreadsheet(file: File): Promise<{ rows: SpreadsheetRow[]; headers: string[] }> {
  try {
    const buffer = await file.arrayBuffer();
    const workbook = XLSX.read(buffer, { type: 'array', cellDates: true });
    const sheet = workbook.Sheets[workbook.SheetNames[0]];
    if (!sheet) throw new Error('A planilha não possui abas com dados.');
    const rows = XLSX.utils.sheet_to_json<SpreadsheetRow>(sheet, { defval: '', raw: false });
    const headers = rows[0] ? Object.keys(rows[0]) : [];
    return { rows, headers };
  } catch (error) {
    throw new Error(
      error instanceof Error ? `Falha de leitura do arquivo: ${error.message}` : 'Falha de leitura do Excel/CSV.',
      { cause: error },
    );
  }
}

function enumValue<T extends readonly string[]>(value: unknown, allowed: T): T[number] | null {
  return allowed.find((item) => normalize(item) === normalize(String(value ?? ''))) ?? null;
}

function getMapped(row: SpreadsheetRow, mapping: ColumnMapping, column: keyof ColumnMapping): unknown {
  const key = mapping[column];
  return key ? row[key] : undefined;
}

function resolveType(row: SpreadsheetRow, mapping: ColumnMapping, signedValue: number): TransactionType | null {
  const explicit = enumValue(getMapped(row, mapping, 'Tipo'), transactionTypes);
  if (explicit) return explicit;
  const debit = parseCurrencyInput(getMapped(row, mapping, 'Débito'));
  const credit = parseCurrencyInput(getMapped(row, mapping, 'Crédito'));
  if (credit && Number(credit) > 0) return 'Receita';
  if (debit && Number(debit) > 0) return 'Despesa';
  if (signedValue < 0) return 'Despesa';
  if (signedValue > 0) return 'Receita';
  return null;
}

function resolveValue(row: SpreadsheetRow, mapping: ColumnMapping): { absolute: string | null; signed: number } {
  const debit = parseCurrencyInput(getMapped(row, mapping, 'Débito'));
  const credit = parseCurrencyInput(getMapped(row, mapping, 'Crédito'));
  if (credit && Number(credit) > 0) return { absolute: credit, signed: Number(credit) };
  if (debit && Number(debit) > 0) return { absolute: debit, signed: -Number(debit) };
  const signed = parseSignedCurrencyInput(getMapped(row, mapping, 'Valor'));
  return { absolute: signed === null ? null : Math.abs(Number(signed)).toFixed(2), signed: Number(signed ?? 0) };
}

export function validateRows(
  rows: SpreadsheetRow[],
  mapping: ColumnMapping,
  existing: Transaction[] = [],
): ImportResult {
  const errors: ImportError[] = [];
  const duplicates: ImportError[] = [];
  const validRows: Transaction[] = [];
  let ignoredRows = 0;
  const required: ColumnKey[] = ['Data', 'Descrição'];
  const hasValueSource = Boolean(mapping.Valor || mapping.Crédito || mapping.Débito);
  required.forEach((column) => {
    if (!mapping[column]) errors.push({ row: 0, field: column, message: `Coluna obrigatória ausente: ${column}`, severity: 'error' });
  });
  if (!hasValueSource) errors.push({ row: 0, field: 'Valor', message: 'Mapeie Valor ou colunas Crédito/Débito.', severity: 'error' });
  if (errors.some((error) => error.row === 0)) return { validRows, errors, duplicates, ignoredRows, totalRows: rows.length };

  const seen = new Set(existing.map((transaction) => transaction.externalId || `${transaction.date}-${normalize(transaction.description)}-${transaction.value}`));

  rows.forEach((row, index) => {
    const rowNumber = index + 2;
    if (Object.values(row).every((value) => String(value ?? '').trim() === '')) {
      ignoredRows += 1;
      return;
    }

    const date = parseDateInput(getMapped(row, mapping, 'Data'));
    const dueDate = parseDateInput(getMapped(row, mapping, 'Vencimento')) ?? undefined;
    const paidAt = parseDateInput(getMapped(row, mapping, 'Pago em')) ?? date ?? undefined;
    const { absolute: value, signed } = resolveValue(row, mapping);
    const type = resolveType(row, mapping, signed);
    const status = enumValue(getMapped(row, mapping, 'Status'), transactionStatuses) ?? 'Pago';
    const paymentMethod = enumValue(getMapped(row, mapping, 'Forma de pagamento'), paymentMethods) ?? 'Transferência';
    const description = String(getMapped(row, mapping, 'Descrição') ?? '').trim();
    const category = String(getMapped(row, mapping, 'Categoria') ?? '').trim() || suggestCategory(description, type ?? 'Despesa');

    if (!date) errors.push({ row: rowNumber, field: 'Data', message: 'Data inválida. Use DD/MM/AAAA.', severity: 'error' });
    if (!value || Number(value) <= 0) errors.push({ row: rowNumber, field: 'Valor', message: 'Valor inválido.', severity: 'error' });
    if (!description) errors.push({ row: rowNumber, field: 'Descrição', message: 'Descrição obrigatória.', severity: 'error' });
    if (!type) errors.push({ row: rowNumber, field: 'Tipo', message: 'Tipo inválido ou impossível de inferir.', severity: 'error' });

    const now = new Date().toISOString();
    const externalId = String(getMapped(row, mapping, 'ID externo') ?? '').trim() || undefined;
    const duplicateKey = externalId || `${date}-${normalize(description)}-${value}`;
    if (seen.has(duplicateKey)) duplicates.push({ row: rowNumber, field: 'ID externo', message: 'Lançamento duplicado detectado e será ignorado.', severity: 'warning' });
    if (errors.some((error) => error.row === rowNumber) || seen.has(duplicateKey) || !type || !date || !value) return;
    seen.add(duplicateKey);

    const transaction: Transaction = {
      id: createId(),
      date,
      description,
      type,
      category,
      subcategory: String(getMapped(row, mapping, 'Subcategoria') ?? '').trim() || undefined,
      value,
      account: String(getMapped(row, mapping, 'Conta') ?? 'Extrato importado').trim() || 'Extrato importado',
      paymentMethod,
      status,
      installment: Number(getMapped(row, mapping, 'Parcela') || 1),
      totalInstallments: Number(getMapped(row, mapping, 'Total de parcelas') || 1),
      dueDate,
      paidAt: status === 'Pago' ? paidAt : undefined,
      notes: String(getMapped(row, mapping, 'Observações') ?? '').trim() || undefined,
      externalId,
      createdAt: now,
      updatedAt: now,
    };
    const parsed = transactionSchema.safeParse(transaction);
    if (parsed.success) validRows.push(transaction);
    else parsed.error.issues.forEach((issue) => errors.push({ row: rowNumber, field: String(issue.path[0] ?? ''), message: issue.message, severity: 'error' }));
  });

  return { validRows, errors, duplicates, ignoredRows, totalRows: rows.length };
}

export function buildTemplateWorkbook(): XLSX.WorkBook {
  const rows = [
    [...spreadsheetColumns],
    ['01/05/2026', 'Salário', 'Receita', 'Salário', 'Mensal', '2500,00', 'Banco', 'Transferência', 'Pago', 1, 1, '01/05/2026', '01/05/2026', 'Salário mensal', 'REC001'],
    ['02/05/2026', 'Mercado', 'Despesa', 'Alimentação', 'Supermercado', '320,50', 'Cartão Nubank', 'Crédito', 'Pago', 1, 1, '10/06/2026', '02/05/2026', 'Compra do mês', 'DES001'],
    ['03/05/2026', 'Internet', 'Despesa', 'Contas da casa', 'Internet', '99,90', 'Banco', 'Pix', 'Pendente', 1, 1, '10/05/2026', '', 'Conta mensal', 'DES002'],
  ];
  const workbook = XLSX.utils.book_new();
  XLSX.utils.book_append_sheet(workbook, XLSX.utils.aoa_to_sheet(rows), 'Modelo FinançasPro');
  XLSX.utils.book_append_sheet(
    workbook,
    XLSX.utils.aoa_to_sheet([
      ['Data', 'Histórico', 'Valor', 'Crédito', 'Débito', 'Saldo'],
      ['04/05/2026', 'PIX RECEBIDO CLIENTE', '', '150,00', '', '150,00'],
      ['05/05/2026', 'UBER TRIP', '-32,90', '', '32,90', '117,10'],
    ]),
    'Exemplo Extrato',
  );
  return workbook;
}
