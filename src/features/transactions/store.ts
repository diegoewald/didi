import { create } from 'zustand';
import type { Account, AppSettings, BackupPayload, Budget, Category, CreditCard, Goal, Transaction } from '../../types';
import { defaultSettings } from '../../constants/defaults';
import { clearAllData, deleteOne, getAll, putMany, putOne, seedDefaultsIfNeeded } from '../../lib/db/localDb';

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
  addTransactions: (items: Transaction[]) => Promise<void>;
  upsertTransaction: (item: Transaction) => Promise<void>;
  deleteTransaction: (id: string) => Promise<void>;
  upsertCategory: (item: Category) => Promise<void>;
  deleteCategory: (id: string, reclassifyTo?: string) => Promise<void>;
  upsertAccount: (item: Account) => Promise<void>;
  upsertBudget: (item: Budget) => Promise<void>;
  upsertGoal: (item: Goal) => Promise<void>;
  restore: (payload: BackupPayload) => Promise<void>;
  wipe: () => Promise<void>;
}

export const useFinanceStore = create<FinanceStore>((set, get) => ({
  transactions: [],
  categories: [],
  accounts: [],
  creditCards: [],
  budgets: [],
  goals: [],
  settings: defaultSettings,
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
      set({ transactions, categories, accounts, creditCards, budgets, goals, settings: settingsRows[0] ?? defaultSettings, loading: false });
    } catch (error) {
      set({ error: error instanceof Error ? error.message : 'Erro ao carregar IndexedDB.', loading: false });
    }
  },
  async addTransactions(items) {
    await putMany('transactions', items);
    set({ transactions: [...get().transactions, ...items] });
  },
  async upsertTransaction(item) {
    await putOne('transactions', item);
    set({ transactions: [...get().transactions.filter((transaction) => transaction.id !== item.id), item] });
  },
  async deleteTransaction(id) {
    await deleteOne('transactions', id);
    set({ transactions: get().transactions.filter((transaction) => transaction.id !== id) });
  },
  async upsertCategory(item) {
    await putOne('categories', item);
    set({ categories: [...get().categories.filter((category) => category.id !== item.id), item] });
  },
  async deleteCategory(id, reclassifyTo = 'Outros') {
    const category = get().categories.find((item) => item.id === id);
    if (!category) return;
    const updated = get().transactions.map((transaction) =>
      transaction.category === category.name ? { ...transaction, category: reclassifyTo, updatedAt: new Date().toISOString() } : transaction,
    );
    await putMany('transactions', updated);
    await deleteOne('categories', id);
    set({ transactions: updated, categories: get().categories.filter((item) => item.id !== id) });
  },
  async upsertAccount(item) {
    await putOne('accounts', item);
    set({ accounts: [...get().accounts.filter((account) => account.id !== item.id), item] });
  },
  async upsertBudget(item) {
    await putOne('budgets', item);
    set({ budgets: [...get().budgets.filter((budget) => budget.id !== item.id), item] });
  },
  async upsertGoal(item) {
    await putOne('goals', item);
    set({ goals: [...get().goals.filter((goal) => goal.id !== item.id), item] });
  },
  async restore(payload) {
    await clearAllData();
    await Promise.all([
      putMany('transactions', payload.transactions),
      putMany('categories', payload.categories),
      putMany('accounts', payload.accounts),
      putMany('creditCards', payload.creditCards),
      putMany('budgets', payload.budgets),
      putMany('goals', payload.goals),
      putOne('settings', { ...payload.settings, id: 'settings' }),
    ]);
    await get().load();
  },
  async wipe() {
    await clearAllData();
    await seedDefaultsIfNeeded();
    await get().load();
  },
}));
