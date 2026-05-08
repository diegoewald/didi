import { describe, expect, it } from 'vitest';
import type { Budget, Transaction } from '../types';
import { deleteById, filterNewTransactions, hasDuplicateBudget, upsertById } from '../lib/crud/collections';
import { calculateBudgetUsage } from '../lib/calculations/finance';
import { createId } from '../lib/utils/id';

const tx = (partial: Partial<Transaction>): Transaction => ({
  id: createId(),
  date: '2026-05-10',
  description: 'Mercado',
  type: 'Despesa',
  category: 'Alimentação',
  value: '100.00',
  account: 'Banco',
  paymentMethod: 'Pix',
  status: 'Pago',
  installment: 1,
  totalInstallments: 1,
  createdAt: new Date().toISOString(),
  updatedAt: new Date().toISOString(),
  ...partial,
});

describe('CRUD helpers V1.5', () => {
  it('cria, edita e exclui metas por id', () => {
    const created = upsertById([], { id: 'goal-1', name: 'Viagem' });
    const edited = upsertById(created, { id: 'goal-1', name: 'Viagem 2026' });
    expect(edited).toEqual([{ id: 'goal-1', name: 'Viagem 2026' }]);
    expect(deleteById(edited, 'goal-1')).toEqual([]);
  });

  it('cria, edita e exclui contas por id', () => {
    const created = upsertById([], { id: 'acc-1', name: 'Banco', active: true });
    const edited = upsertById(created, { id: 'acc-1', name: 'Banco Principal', active: false });
    expect(edited[0].active).toBe(false);
    expect(deleteById(edited, 'acc-1')).toHaveLength(0);
  });

  it('cria, edita e exclui cartões por id', () => {
    const created = upsertById([], { id: 'card-1', name: 'Cartão A', limit: '1000' });
    const edited = upsertById(created, { id: 'card-1', name: 'Cartão B', limit: '2000' });
    expect(edited[0].limit).toBe('2000');
    expect(deleteById(edited, 'card-1')).toHaveLength(0);
  });

  it('impede orçamento duplicado para mesma categoria e mês', () => {
    const existing: Budget[] = [{ id: 'b1', month: '2026-05', category: 'Alimentação', limit: '800' }];
    expect(hasDuplicateBudget(existing, { id: 'b2', month: '2026-05', category: 'Alimentação', limit: '900' })).toBe(true);
    expect(hasDuplicateBudget(existing, { id: 'b1', month: '2026-05', category: 'Alimentação', limit: '900' })).toBe(false);
  });

  it('calcula orçamento usado por categoria', () => {
    const budget: Budget = { id: 'b1', month: '2026-05', category: 'Alimentação', limit: '400' };
    expect(calculateBudgetUsage(budget, [tx({ value: '100.00' }), tx({ value: '50.00', status: 'Pendente' })])).toBe(37.5);
  });

  it('não duplica transações ao importar a mesma prévia novamente', () => {
    const first = tx({ id: 't1', externalId: 'EXT-1' });
    const second = tx({ id: 't2', externalId: 'EXT-1' });
    expect(filterNewTransactions([second], [first])).toEqual([]);
  });
});
