import { format, isValid, parse } from 'date-fns';
import { ptBR } from 'date-fns/locale';

export const currencyFormatter = new Intl.NumberFormat('pt-BR', {
  style: 'currency',
  currency: 'BRL',
});
export const numberFormatter = new Intl.NumberFormat('pt-BR', {
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
});

export function formatCurrency(value: string | number): string {
  return currencyFormatter.format(Number(value || 0));
}

export function formatDateBR(date?: string): string {
  if (!date) return '—';
  const parsed = new Date(`${date}T00:00:00`);
  return isValid(parsed) ? format(parsed, 'dd/MM/yyyy', { locale: ptBR }) : '—';
}

export function parseDateInput(input: unknown): string | null {
  if (input instanceof Date && isValid(input)) return format(input, 'yyyy-MM-dd');
  const value = String(input ?? '').trim();
  if (!value) return null;
  const patterns = ['dd/MM/yyyy', 'd/M/yyyy', 'yyyy-MM-dd', 'dd-MM-yyyy', 'MM/dd/yyyy'];
  for (const pattern of patterns) {
    const parsed = parse(value, pattern, new Date());
    if (isValid(parsed)) return format(parsed, 'yyyy-MM-dd');
  }
  const excelSerial = Number(value);
  if (Number.isFinite(excelSerial) && excelSerial > 20_000 && excelSerial < 80_000) {
    return format(new Date(Math.round((excelSerial - 25_569) * 86_400 * 1000)), 'yyyy-MM-dd');
  }
  return null;
}

export function parseSignedCurrencyInput(input: unknown): string | null {
  if (typeof input === 'number' && Number.isFinite(input)) return input.toFixed(2);
  const raw = String(input ?? '')
    .replace(/R\$|\s/g, '')
    .replace(/[()]/g, (match) => (match === '(' ? '-' : ''))
    .trim();
  if (!raw) return null;
  const normalized = raw.includes(',') ? raw.replace(/\./g, '').replace(',', '.') : raw;
  const value = Number(normalized);
  return Number.isFinite(value) ? value.toFixed(2) : null;
}

export function parseCurrencyInput(input: unknown): string | null {
  const signed = parseSignedCurrencyInput(input);
  return signed === null ? null : Math.abs(Number(signed)).toFixed(2);
}

export function downloadBlob(blob: Blob, filename: string): void {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = filename;
  a.click();
  URL.revokeObjectURL(url);
}

export function safeFileDate(date = new Date()): string {
  return format(date, 'dd-MM-yyyy');
}
