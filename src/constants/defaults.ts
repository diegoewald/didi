import type { Account, AppSettings, Category, CreditCard, Goal, Budget } from '../types';

const colors = ['#14b8a6','#f97316','#6366f1','#ef4444','#8b5cf6','#ec4899','#22c55e','#0ea5e9','#f59e0b','#64748b'];
const names = ['Alimentação','Transporte','Moradia','Saúde','Educação','Lazer','Mercado','Contas da casa','Assinaturas','Compras','Salário','Renda extra','Investimentos','Emergência','Outros'];
export const defaultCategories: Category[] = names.map((name, index) => ({ id: `cat-${index}`, name, color: colors[index % colors.length], icon: ['Utensils','Car','Home','HeartPulse','GraduationCap','Sparkles','ShoppingCart','Receipt','Repeat','ShoppingBag','Wallet','Plus','TrendingUp','Shield','CircleDollarSign'][index], subcategories: [], isDefault: true }));
export const defaultAccounts: Account[] = [
  { id: 'acc-banco', name: 'Banco', type: 'Banco', initialBalance: '0', color: '#14b8a6', active: true },
  { id: 'acc-carteira', name: 'Carteira', type: 'Carteira', initialBalance: '0', color: '#f59e0b', active: true },
  { id: 'acc-cartao', name: 'Cartão Nubank', type: 'Cartão de crédito', initialBalance: '0', color: '#7c3aed', active: true },
];
export const defaultCreditCards: CreditCard[] = [{ id: 'card-nubank', name: 'Cartão Nubank', limit: '5000', bestPurchaseDay: 11, closingDay: 10, dueDay: 17, accountName: 'Cartão Nubank', color: '#7c3aed' }];
export const defaultBudgets: Budget[] = [];
export const defaultGoals: Goal[] = [{ id: 'goal-emergencia', name: 'Reserva de emergência', targetValue: '10000', currentValue: '1250', targetDate: '2026-12-31', category: 'Emergência', color: '#22c55e' }];
export const defaultSettings: AppSettings = { currency: 'BRL', theme: 'system', dateFormat: 'dd/MM/yyyy', financialMonthStart: 1, cashView: 'caixa' };
export const spreadsheetColumns = ['Data','Descrição','Tipo','Categoria','Subcategoria','Valor','Conta','Forma de pagamento','Status','Parcela','Total de parcelas','Vencimento','Pago em','Observações','ID externo'] as const;
