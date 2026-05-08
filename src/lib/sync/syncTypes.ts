import type { Account, AppSettings, Budget, Category, CreditCard, Goal, Transaction } from '../../types';

export type SyncCollection = 'transactions' | 'categories' | 'accounts' | 'creditCards' | 'budgets' | 'goals' | 'settings';
export type SyncStatus = 'local' | 'online' | 'syncing' | 'synced' | 'error' | 'offline';
export type SyncAction = 'upsert' | 'delete';

export interface SyncEntityMap {
  transactions: Transaction;
  categories: Category;
  accounts: Account;
  creditCards: CreditCard;
  budgets: Budget;
  goals: Goal;
  settings: AppSettings;
}

export type SyncEntity = SyncEntityMap[SyncCollection];

export interface SyncOperation<K extends SyncCollection = SyncCollection> {
  id: string;
  collection: K;
  action: SyncAction;
  recordId: string;
  payload?: SyncEntityMap[K];
  updatedAt: string;
  attempts: number;
}

export interface SyncSnapshot {
  transactions: Transaction[];
  categories: Category[];
  accounts: Account[];
  creditCards: CreditCard[];
  budgets: Budget[];
  goals: Goal[];
  settings: AppSettings[];
}

export interface MigrationSummary {
  transactions: number;
  categories: number;
  accounts: number;
  creditCards: number;
  budgets: number;
  goals: number;
  settings: number;
}
