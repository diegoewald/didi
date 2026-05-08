import { useState } from 'react';
import type { Account, AccountType } from '../../types';
import { calculateBalance } from '../../lib/calculations/finance';
import { parseCurrencyInput, formatCurrency } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

const accountTypes: AccountType[] = ['Banco', 'Carteira', 'Cartão de crédito', 'Investimento', 'Dinheiro', 'Conta digital', 'Outro'];
const empty = (): Account => ({ id: crypto.randomUUID(), name: '', type: 'Banco', initialBalance: '0', color: '#14b8a6', active: true });

export function AccountsPage() {
  const { accounts, transactions, upsertAccount, deleteAccount } = useFinanceStore();
  const [form, setForm] = useState<Account>(empty());
  const [editing, setEditing] = useState(false);
  const linked = (accountName: string) => transactions.some((transaction) => transaction.account === accountName);
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.name.trim()) return alert('Informe o nome da conta.');
    const value = parseCurrencyInput(form.initialBalance) ?? '0.00';
    try {
      await upsertAccount({ ...form, name: form.name.trim(), initialBalance: value });
      alert(editing ? 'Conta editada com sucesso.' : 'Conta salva com sucesso.');
      setForm(empty());
      setEditing(false);
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Erro ao salvar conta.');
    }
  };
  const remove = async (account: Account) => {
    let moveTo: string | undefined;
    if (linked(account.name)) {
      const candidates = accounts.filter((item) => item.id !== account.id);
      if (candidates.length === 0) return alert('Crie outra conta antes de excluir uma conta com lançamentos vinculados.');
      const choice = window.prompt(`A conta possui lançamentos. Digite a conta de destino:\n${candidates.map((item) => item.name).join(', ')}`);
      if (!choice || !candidates.some((item) => item.name === choice)) return alert('Exclusão cancelada: conta de destino inválida.');
      moveTo = choice;
    }
    if (!window.confirm(`Excluir a conta "${account.name}"?`)) return;
    await deleteAccount(account.id, moveTo);
    alert('Conta excluída com sucesso.');
  };
  return <div className="space-y-6"><h1 className="text-3xl font-black">Contas e carteiras</h1><form onSubmit={save} className="card grid gap-3 p-5 md:grid-cols-6"><input className="input md:col-span-2" placeholder="Nome da conta" value={form.name} onChange={(event)=>setForm({...form,name:event.target.value})}/><select className="input" value={form.type} onChange={(event)=>setForm({...form,type:event.target.value as AccountType})}>{accountTypes.map((type)=><option key={type}>{type}</option>)}</select><input className="input" placeholder="Saldo inicial" value={form.initialBalance} onChange={(event)=>setForm({...form,initialBalance:event.target.value})}/><input className="input" type="color" value={form.color} onChange={(event)=>setForm({...form,color:event.target.value})}/><label className="flex items-center gap-2 font-bold"><input type="checkbox" checked={form.active} onChange={(event)=>setForm({...form,active:event.target.checked})}/>Ativa</label><div className="flex gap-2 md:col-span-6"><button className="btn btn-primary" type="submit">{editing?'Salvar alterações':'Adicionar conta'}</button>{editing && <button className="btn btn-secondary" type="button" onClick={()=>{setForm(empty());setEditing(false);}}>Cancelar</button>}</div></form><div className="grid gap-4 md:grid-cols-3">{accounts.map((account)=><section key={account.id} className="card p-5"><div className="flex justify-between gap-3"><b>{account.name}</b><span className="badge">{account.type}</span></div><p className="mt-4 text-sm text-slate-600 dark:text-slate-300">Saldo atual calculado</p><h2 className="text-2xl font-black">{formatCurrency(calculateBalance(transactions.filter((transaction)=>transaction.account===account.name), account.initialBalance))}</h2><p className="mt-2 text-sm"><span className="inline-block h-3 w-3 rounded-full" style={{background:account.color}}/> {account.active?'Ativa':'Inativa'}</p><div className="mt-4 flex gap-2"><button className="btn btn-secondary" onClick={()=>{setForm(account);setEditing(true);}}>Editar</button><button className="btn btn-danger" onClick={()=>remove(account)}>Excluir</button></div></section>)}</div></div>;
}
