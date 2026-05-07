import type { Budget, Transaction } from '../../types';

export function upsertById<T extends { id: string }>(items: T[], item: T): T[] {
  return [...items.filter((current) => current.id !== item.id), item];
}

export function deleteById<T extends { id: string }>(items: T[], id: string): T[] {
  return items.filter((item) => item.id !== id);
}

export function uniqueBudgetKey(budget: Pick<Budget, 'month' | 'category'>): string {
  return `${budget.month}::${budget.category.trim().toLocaleLowerCase('pt-BR')}`;
}

export function hasDuplicateBudget(budgets: Budget[], budget: Budget): boolean {
  const key = uniqueBudgetKey(budget);
  return budgets.some((item) => item.id !== budget.id && uniqueBudgetKey(item) === key);
}

export function transactionDedupKey(transaction: Transaction): string {
  return transaction.externalId || `${transaction.date}-${transaction.description.trim().toLocaleLowerCase('pt-BR')}-${transaction.value}`;
}

export function filterNewTransactions(incoming: Transaction[], existing: Transaction[]): Transaction[] {
  const seen = new Set(existing.map(transactionDedupKey));
  const accepted: Transaction[] = [];
  for (const transaction of incoming) {
    const key = transactionDedupKey(transaction);
    if (seen.has(key)) continue;
    seen.add(key);
    accepted.push(transaction);
  }
  return accepted;
}
