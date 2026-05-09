import { create } from 'zustand';
import type { Account, AppSettings, BackupPayload, Budget, Category, CreditCard, Goal, Transaction } from '../../types';
import type { SyncCollection, SyncEntityMap, SyncSnapshot } from '../../lib/sync/syncTypes';
import { defaultSettings } from '../../constants/defaults';
import { clearAllData, clearStore, deleteOne, getAll, putMany, putOne, seedDefaultsIfNeeded, type StoreName } from '../../lib/db/localDb';
import { deleteById, filterNewTransactions, hasDuplicateBudget, upsertById } from '../../lib/crud/collections';
import { enqueueSyncOperation } from '../../lib/sync/syncQueue';
import { flushSyncQueue } from '../../lib/sync/syncService';
import { getStoredSession, isSupabaseConfigured } from '../../lib/supabase/client';


function queueSync<K extends SyncCollection>(collection: K, action: 'upsert' | 'delete', recordId: string, payload?: SyncEntityMap[K]): void {
  enqueueSyncOperation(collection, action, recordId, payload);
  if (isSupabaseConfigured() && getStoredSession()) void flushSyncQueue().catch(() => undefined);
}

function readCachedSettings(): AppSettings {
  try {
    const cached = localStorage.getItem('financaspro-settings');
    return cached ? { ...defaultSettings, ...JSON.parse(cached), id: 'settings' } : defaultSettings;
  } catch (error) {
    console.warn('Não foi possível ler tema em cache.', error);
    return defaultSettings;
  }
}

function cacheSettings(settings: AppSettings): void {
  try {
    localStorage.setItem('financaspro-settings', JSON.stringify(settings));
  } catch (error) {
    console.warn('Não foi possível salvar tema em cache.', error);
  }
}

interface FinanceStore {
  transactions: Transaction[];
  categories: Category[];
  accounts: Account[];
  creditCards: CreditCard[];
  budgets: Budget[];
  goals: Goal[];
  settings: AppSettings;
  loading: boolean;
  error?: string;
  load: () => Promise<void>;
  addTransactions: (items: Transaction[]) => Promise<number>;
  upsertTransaction: (item: Transaction) => Promise<void>;
  deleteTransaction: (id: string) => Promise<void>;
  upsertCategory: (item: Category) => Promise<void>;
  deleteCategory: (id: string, reclassifyTo?: string) => Promise<void>;
  upsertAccount: (item: Account) => Promise<void>;
  deleteAccount: (id: string, moveTransactionsTo?: string) => Promise<void>;
  upsertBudget: (item: Budget) => Promise<void>;
  deleteBudget: (id: string) => Promise<void>;
  upsertGoal: (item: Goal) => Promise<void>;
  deleteGoal: (id: string) => Promise<void>;
  upsertCreditCard: (item: CreditCard) => Promise<void>;
  deleteCreditCard: (id: string) => Promise<void>;
  upsertSettings: (settings: AppSettings) => Promise<void>;
  restore: (payload: BackupPayload) => Promise<void>;
  snapshot: () => SyncSnapshot;
  applySnapshot: (snapshot: SyncSnapshot) => Promise<void>;
  wipe: () => Promise<void>;
}

export const useFinanceStore = create<FinanceStore>((set, get) => ({
  transactions: [],
  categories: [],
  accounts: [],
  creditCards: [],
  budgets: [],
  goals: [],
  settings: readCachedSettings(),
  loading: false,
  async load() {
    set({ loading: true, error: undefined });
    try {
      await seedDefaultsIfNeeded();
      const [transactions, categories, accounts, creditCards, budgets, goals, settingsRows] = await Promise.all([
        getAll('transactions'),
        getAll('categories'),
        getAll('accounts'),
        getAll('creditCards'),
        getAll('budgets'),
        getAll('goals'),
        getAll('settings'),
      ]);
      set({ transactions, categories, accounts, creditCards, budgets, goals, settings: settingsRows[0] ?? readCachedSettings(), loading: false });
    } catch (error) {
      console.error('Erro ao carregar IndexedDB:', error);
      set({ error: 'Não foi possível abrir seus dados locais no IndexedDB. Recarregue a página ou restaure um backup se o problema continuar.', loading: false });
    }
  },
  async addTransactions(items) {
    const accepted = filterNewTransactions(items, get().transactions);
    if (accepted.length === 0) return 0;
    await putMany('transactions', accepted);
    set({ transactions: [...get().transactions, ...accepted] });
    accepted.forEach((item) => queueSync('transactions', 'upsert', item.id, item));
    return accepted.length;
  },
  async upsertTransaction(item) {
    await putOne('transactions', item);
    set({ transactions: upsertById(get().transactions, item) });
    queueSync('transactions', 'upsert', item.id, item);
  },
  async deleteTransaction(id) {
    await deleteOne('transactions', id);
    set({ transactions: deleteById(get().transactions, id) });
    queueSync('transactions', 'delete', id);
  },
  async upsertCategory(item) {
    await putOne('categories', item);
    set({ categories: upsertById(get().categories, item) });
    queueSync('categories', 'upsert', item.id, item);
  },
  async deleteCategory(id, reclassifyTo = 'Outros') {
    const category = get().categories.find((item) => item.id === id);
    if (!category) return;
    const updated = get().transactions.map((transaction) =>
      transaction.category === category.name ? { ...transaction, category: reclassifyTo, updatedAt: new Date().toISOString() } : transaction,
    );
    await putMany('transactions', updated);
    await deleteOne('categories', id);
    set({ transactions: updated, categories: deleteById(get().categories, id) });
    updated.forEach((transaction) => queueSync('transactions', 'upsert', transaction.id, transaction));
    queueSync('categories', 'delete', id);
  },
  async upsertAccount(item) {
    await putOne('accounts', item);
    set({ accounts: upsertById(get().accounts, item) });
    queueSync('accounts', 'upsert', item.id, item);
  },
  async deleteAccount(id, moveTransactionsTo) {
    const account = get().accounts.find((item) => item.id === id);
    if (!account) return;
    let updatedTransactions = get().transactions;
    if (moveTransactionsTo) {
      updatedTransactions = updatedTransactions.map((transaction) =>
        transaction.account === account.name ? { ...transaction, account: moveTransactionsTo, updatedAt: new Date().toISOString() } : transaction,
      );
      await putMany('transactions', updatedTransactions);
    }
    await deleteOne('accounts', id);
    set({ accounts: deleteById(get().accounts, id), transactions: updatedTransactions });
    updatedTransactions.forEach((transaction) => queueSync('transactions', 'upsert', transaction.id, transaction));
    queueSync('accounts', 'delete', id);
  },
  async upsertBudget(item) {
    if (hasDuplicateBudget(get().budgets, item)) throw new Error('Já existe orçamento para esta categoria neste mês.');
    await putOne('budgets', item);
    set({ budgets: upsertById(get().budgets, item) });
    queueSync('budgets', 'upsert', item.id, item);
  },
  async deleteBudget(id) {
    await deleteOne('budgets', id);
    set({ budgets: deleteById(get().budgets, id) });
    queueSync('budgets', 'delete', id);
  },
  async upsertGoal(item) {
    await putOne('goals', item);
    set({ goals: upsertById(get().goals, item) });
    queueSync('goals', 'upsert', item.id, item);
  },
  async deleteGoal(id) {
    await deleteOne('goals', id);
    set({ goals: deleteById(get().goals, id) });
    queueSync('goals', 'delete', id);
  },
  async upsertCreditCard(item) {
    await putOne('creditCards', item);
    set({ creditCards: upsertById(get().creditCards, item) });
    queueSync('creditCards', 'upsert', item.id, item);
  },
  async deleteCreditCard(id) {
    await deleteOne('creditCards', id);
    set({ creditCards: deleteById(get().creditCards, id) });
    queueSync('creditCards', 'delete', id);
  },
  async upsertSettings(settings) {
    const next = { ...settings, id: 'settings' };
    await putOne('settings', next);
    cacheSettings(next);
    set({ settings: next });
    queueSync('settings', 'upsert', 'settings', next);
  },
  async restore(payload) {
    await clearAllData();
    const restoredSettings = { ...payload.settings, id: 'settings' };
    await Promise.all([
      putMany('transactions', payload.transactions),
      putMany('categories', payload.categories),
      putMany('accounts', payload.accounts),
      putMany('creditCards', payload.creditCards),
      putMany('budgets', payload.budgets),
      putMany('goals', payload.goals),
      putOne('settings', restoredSettings),
    ]);
    cacheSettings(restoredSettings);
    await get().load();
  },
  snapshot() {
    return { transactions: get().transactions, categories: get().categories, accounts: get().accounts, creditCards: get().creditCards, budgets: get().budgets, goals: get().goals, settings: [get().settings] };
  },
  async applySnapshot(snapshot) {
    const settings = snapshot.settings[0] ?? get().settings;
    const stores: StoreName[] = ['transactions', 'categories', 'accounts', 'creditCards', 'budgets', 'goals', 'settings'];
    await Promise.all(stores.map((store) => clearStore(store)));
    await Promise.all([
      putMany('transactions', snapshot.transactions),
      putMany('categories', snapshot.categories),
      putMany('accounts', snapshot.accounts),
      putMany('creditCards', snapshot.creditCards),
      putMany('budgets', snapshot.budgets),
      putMany('goals', snapshot.goals),
      putOne('settings', settings),
    ]);
    cacheSettings(settings);
    set({ ...snapshot, settings });
  },
  async wipe() {
    await clearAllData();
    await seedDefaultsIfNeeded();
    cacheSettings({ ...defaultSettings, id: 'settings' });
    await get().load();
  },
}));
