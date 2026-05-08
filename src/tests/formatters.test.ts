import { describe, expect, it } from 'vitest';
import { parseCurrencyInput, parseDateInput, parseSignedCurrencyInput } from '../lib/formatters/formatters';

describe('parsing brasileiro de moeda e data', () => {
  it('converte Real brasileiro com separadores e símbolo', () => {
    expect(parseCurrencyInput('R$ 1.234,56')).toBe('1234.56');
    expect(parseCurrencyInput('(R$ 89,90)')).toBe('89.90');
  });

  it('preserva sinal quando o extrato usa despesas negativas', () => {
    expect(parseSignedCurrencyInput('-32,90')).toBe('-32.90');
    expect(parseSignedCurrencyInput('(32,90)')).toBe('-32.90');
  });

  it('converte datas brasileiras e serial Excel para ISO', () => {
    expect(parseDateInput('08/05/2026')).toBe('2026-05-08');
    expect(parseDateInput(46_420)).toBe('2027-02-02');
  });
});
