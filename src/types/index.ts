export type TransactionType = 'Receita' | 'Despesa' | 'Transferência';
export type TransactionStatus = 'Pago' | 'Pendente' | 'Atrasado' | 'Cancelado';
export type PaymentMethod = 'Pix' | 'Crédito' | 'Débito' | 'Dinheiro' | 'Boleto' | 'Transferência';
export type AccountType = 'Banco' | 'Dinheiro' | 'Carteira' | 'Cartão de crédito' | 'Conta digital' | 'Investimento';

export interface Transaction {
  id: string;
  date: string;
  description: string;
  type: TransactionType;
  category: string;
  subcategory?: string;
  value: string;
  account: string;
  paymentMethod: PaymentMethod;
  status: TransactionStatus;
  installment: number;
  totalInstallments: number;
  dueDate?: string;
  paidAt?: string;
  notes?: string;
  externalId?: string;
  createdAt: string;
  updatedAt: string;
}
export interface Subcategory { id: string; name: string; }
export interface Category { id: string; name: string; color: string; icon: string; subcategories: Subcategory[]; isDefault?: boolean; }
export interface Account { id: string; name: string; type: AccountType; initialBalance: string; color: string; active: boolean; }
export interface CreditCard { id: string; name: string; limit: string; bestPurchaseDay: number; closingDay: number; dueDay: number; accountName: string; color: string; }
export interface Budget { id: string; month: string; category: string; limit: string; }
export interface Goal { id: string; name: string; targetValue: string; currentValue: string; targetDate: string; category: string; color: string; }
export interface ImportError { row: number; field?: string; message: string; severity: 'error' | 'warning'; }
export interface SpreadsheetRow { [key: string]: unknown; }
export interface ImportResult { validRows: Transaction[]; errors: ImportError[]; duplicates: ImportError[]; ignoredRows: number; totalRows: number; }
export interface FinancialSummary { balance: string; monthlyIncome: string; monthlyExpense: string; monthlyNet: string; savingsRate: number; healthScore: number; previousMonthNet: string; }
export interface MonthlyReport { month: string; income: string; expense: string; net: string; balance: string; }
export interface AppSettings { currency: 'BRL'; theme: 'light' | 'dark' | 'system'; dateFormat: 'dd/MM/yyyy'; financialMonthStart: number; cashView: 'caixa' | 'competencia'; }
export interface BackupPayload { version: 1; exportedAt: string; transactions: Transaction[]; categories: Category[]; accounts: Account[]; creditCards: CreditCard[]; budgets: Budget[]; goals: Goal[]; settings: AppSettings; }
