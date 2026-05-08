import { describe, expect, it } from 'vitest';
import { detectColumnMapping, suggestCategory, validateRows } from '../lib/spreadsheet/importer';

describe('importação de planilha', () => {
  it('detecta mapeamento inteligente por sinônimos', () => {
    const mapping = detectColumnMapping(['lançamento', 'historico', 'vlr', 'grupo', 'pagamento', 'status', 'tipo', 'conta']);
    expect(mapping.Data).toBe('lançamento');
    expect(mapping.Descrição).toBe('historico');
    expect(mapping.Valor).toBe('vlr');
    expect(mapping.Categoria).toBe('grupo');
  });

  it('sugere categorias por descrição', () => {
    expect(suggestCategory('Uber viagem', 'Despesa')).toBe('Transporte');
    expect(suggestCategory('Netflix mensal', 'Despesa')).toBe('Assinaturas');
  });

  it('valida linhas e detecta erros', () => {
    const rows = [
      { Data: '01/05/2026', Descrição: 'Salário', Tipo: 'Receita', Categoria: 'Salário', Valor: '2500,00', Conta: 'Banco', 'Forma de pagamento': 'Transferência', Status: 'Pago', 'ID externo': 'REC001' },
      { Data: 'ruim', Descrição: '', Tipo: 'Errado', Categoria: '', Valor: 'abc', Conta: 'Banco', 'Forma de pagamento': 'Pix', Status: 'Pago' },
    ];
    const mapping = detectColumnMapping(Object.keys(rows[0]));
    const result = validateRows(rows, mapping, []);
    expect(result.validRows).toHaveLength(1);
    expect(result.errors.length).toBeGreaterThan(0);
    expect(result.validRows[0].value).toBe('2500.00');
  });



  it('aceita extratos com colunas separadas de crédito e débito', () => {
    const rows = [
      { Data: '06/05/2026', Histórico: 'PIX CLIENTE', Crédito: '250,00', Débito: '', Saldo: '250,00' },
      { Data: '07/05/2026', Histórico: 'MERCADO BAIRRO', Crédito: '', Débito: '89,90', Saldo: '160,10' },
    ];
    const result = validateRows(rows, detectColumnMapping(Object.keys(rows[0])), []);
    expect(result.validRows).toHaveLength(2);
    expect(result.validRows[0].type).toBe('Receita');
    expect(result.validRows[1].type).toBe('Despesa');
    expect(result.validRows[1].value).toBe('89.90');
  });

  it('aceita extratos bancários com valor negativo e histórico', () => {
    const rows = [{ Data: '05/05/2026', Histórico: 'UBER TRIP', Valor: '-32,90', Saldo: '117,10' }];
    const result = validateRows(rows, detectColumnMapping(Object.keys(rows[0])), []);
    expect(result.validRows).toHaveLength(1);
    expect(result.validRows[0].type).toBe('Despesa');
    expect(result.validRows[0].category).toBe('Transporte');
    expect(result.validRows[0].value).toBe('32.90');
  });
});
