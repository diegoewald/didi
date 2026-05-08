import { useEffect, useState } from 'react';
import type { Category, PaymentMethod, Transaction, TransactionStatus, TransactionType } from '../../types';
import { parseCurrencyInput } from '../../lib/formatters/formatters';
import { createId } from '../../lib/utils/id';

const types: TransactionType[] = ['Receita', 'Despesa', 'Transferência'];
const statuses: TransactionStatus[] = ['Pago', 'Pendente', 'Atrasado', 'Cancelado'];
const methods: PaymentMethod[] = ['Pix', 'Crédito', 'Débito', 'Dinheiro', 'Boleto', 'Transferência'];

type FormState = {
  date: string;
  description: string;
  type: TransactionType;
  category: string;
  subcategory: string;
  value: string;
  account: string;
  paymentMethod: PaymentMethod;
  status: TransactionStatus;
  dueDate: string;
  notes: string;
};

function emptyForm(): FormState {
  const today = new Date().toISOString().slice(0, 10);
  return {
    date: today,
    description: '',
    type: 'Despesa',
    category: 'Outros',
    subcategory: '',
    value: '',
    account: 'Banco',
    paymentMethod: 'Pix',
    status: 'Pago',
    dueDate: today,
    notes: '',
  };
}

function fromTransaction(transaction: Transaction): FormState {
  return {
    date: transaction.date,
    description: transaction.description,
    type: transaction.type,
    category: transaction.category,
    subcategory: transaction.subcategory ?? '',
    value: transaction.value.replace('.', ','),
    account: transaction.account,
    paymentMethod: transaction.paymentMethod,
    status: transaction.status,
    dueDate: transaction.dueDate ?? transaction.date,
    notes: transaction.notes ?? '',
  };
}

export function TransactionForm({
  categories,
  editing,
  onCancelEdit,
  onSave,
}: {
  categories: Category[];
  editing?: Transaction | null;
  onCancelEdit?: () => void;
  onSave: (transaction: Transaction) => void;
}) {
  const [form, setForm] = useState<FormState>(emptyForm());

  useEffect(() => {
    setForm(editing ? fromTransaction(editing) : emptyForm());
  }, [editing]);

  const submit = (event: React.FormEvent) => {
    event.preventDefault();
    const value = parseCurrencyInput(form.value);
    if (!value) {
      alert('Informe um valor válido.');
      return;
    }
    const now = new Date().toISOString();
    onSave({
      ...form,
      id: editing?.id ?? createId(),
      value,
      installment: editing?.installment ?? 1,
      totalInstallments: editing?.totalInstallments ?? 1,
      paidAt: form.status === 'Pago' ? (editing?.paidAt ?? form.date) : undefined,
      externalId: editing?.externalId,
      createdAt: editing?.createdAt ?? now,
      updatedAt: now,
    });
    if (!editing) setForm(emptyForm());
  };

  const input = (key: keyof FormState) =>
    (event: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) =>
      setForm({ ...form, [key]: event.target.value });

  return (
    <form onSubmit={submit} className="card grid gap-4 p-5 sm:p-6 md:grid-cols-3">
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Data</label>
        <input className="input" type="date" value={form.date} onChange={input('date')} required />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45 md:col-span-2">
        <label className="label">Descrição</label>
        <input className="input" value={form.description} onChange={input('description')} required placeholder="Ex.: Mercado, salário, Uber" />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Tipo</label>
        <select className="input" value={form.type} onChange={input('type')}>{types.map((value) => <option key={value}>{value}</option>)}</select>
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Categoria</label>
        <select className="input" value={form.category} onChange={input('category')}>{categories.map((category) => <option key={category.id}>{category.name}</option>)}</select>
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Subcategoria</label>
        <input className="input" value={form.subcategory} onChange={input('subcategory')} placeholder="Opcional" />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Valor</label>
        <input className="input" inputMode="decimal" value={form.value} onChange={input('value')} placeholder="320,50" required />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Conta</label>
        <input className="input" value={form.account} onChange={input('account')} />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Forma de pagamento</label>
        <select className="input" value={form.paymentMethod} onChange={input('paymentMethod')}>{methods.map((value) => <option key={value}>{value}</option>)}</select>
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Status</label>
        <select className="input" value={form.status} onChange={input('status')}>{statuses.map((value) => <option key={value}>{value}</option>)}</select>
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45">
        <label className="label">Vencimento</label>
        <input className="input" type="date" value={form.dueDate} onChange={input('dueDate')} />
      </div>
      <div className="rounded-2xl bg-slate-50/70 p-3 dark:bg-slate-900/45 md:col-span-2">
        <label className="label">Observações</label>
        <input className="input" value={form.notes} onChange={input('notes')} placeholder="Campo livre" />
      </div>
      <div className="flex flex-col gap-3 pt-2 sm:flex-row md:col-span-3">
        <button className="btn btn-primary" type="submit">{editing ? 'Salvar alterações' : 'Salvar lançamento'}</button>
        {editing && <button className="btn btn-secondary" type="button" onClick={onCancelEdit}>Cancelar edição</button>}
      </div>
    </form>
  );
}
