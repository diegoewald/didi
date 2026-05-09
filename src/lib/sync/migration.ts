import { defaultAccounts, defaultCategories, defaultCreditCards, defaultGoals } from '../../constants/defaults';
import type { MigrationSummary, SyncSnapshot } from './syncTypes';
import { dedupeTransactionsByIdentity, mergeByLatest } from './merge';

export type MigrationDecision = 'migrated' | 'local-only';

const defaultCategoryIds = new Set(defaultCategories.map((item) => item.id));
const defaultAccountIds = new Set(defaultAccounts.map((item) => item.id));
const defaultCreditCardIds = new Set(defaultCreditCards.map((item) => item.id));
const defaultGoalIds = new Set(defaultGoals.map((item) => item.id));

function key(userId: string): string {
  return `financaspro-migration-decision:${userId}`;
}

export function getMigrationDecision(userId: string): MigrationDecision | null {
  try {
    const value = localStorage.getItem(key(userId));
    return value === 'migrated' || value === 'local-only' ? value : null;
  } catch (error) {
    console.warn('Não foi possível ler decisão de migração.', error);
    return null;
  }
}

export function setMigrationDecision(userId: string, decision: MigrationDecision): void {
  try {
    localStorage.setItem(key(userId), decision);
  } catch (error) {
    console.warn('Não foi possível salvar decisão de migração.', error);
  }
}

export function summarizeSnapshot(snapshot: SyncSnapshot): MigrationSummary {
  return {
    transactions: snapshot.transactions.length,
    categories: snapshot.categories.length,
    accounts: snapshot.accounts.length,
    creditCards: snapshot.creditCards.length,
    budgets: snapshot.budgets.length,
    goals: snapshot.goals.length,
    settings: snapshot.settings.length,
  };
}

export function formatMigrationSummary(summary: MigrationSummary): string {
  return `${summary.transactions} lançamentos, ${summary.categories} categorias, ${summary.accounts} contas, ${summary.creditCards} cartões, ${summary.budgets} orçamentos, ${summary.goals} metas e ${summary.settings} configuração(ões)`;
}

export function hasMigratableLocalData(snapshot: SyncSnapshot): boolean {
  return (
    snapshot.transactions.length > 0 ||
    snapshot.budgets.length > 0 ||
    snapshot.categories.some((item) => !defaultCategoryIds.has(item.id)) ||
    snapshot.accounts.some((item) => !defaultAccountIds.has(item.id)) ||
    snapshot.creditCards.some((item) => !defaultCreditCardIds.has(item.id)) ||
    snapshot.goals.some((item) => !defaultGoalIds.has(item.id))
  );
}

export function prepareMigrationSnapshot(local: SyncSnapshot, remote: SyncSnapshot): SyncSnapshot {
  return {
    transactions: dedupeTransactionsByIdentity(mergeByLatest(local.transactions, remote.transactions)),
    categories: mergeByLatest(local.categories, remote.categories),
    accounts: mergeByLatest(local.accounts, remote.accounts),
    creditCards: mergeByLatest(local.creditCards, remote.creditCards),
    budgets: mergeByLatest(local.budgets, remote.budgets),
    goals: mergeByLatest(local.goals, remote.goals),
    settings: mergeByLatest(local.settings, remote.settings),
  };
}
