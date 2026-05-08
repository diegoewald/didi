import { useState } from 'react';
import Decimal from 'decimal.js';
import type { Budget } from '../../types';
import { calculateBudgetUsage } from '../../lib/calculations/finance';
import { parseCurrencyInput, formatCurrency } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

const empty = (): Budget => ({ id: crypto.randomUUID(), month: new Date().toISOString().slice(0, 7), category: 'Alimentação', limit: '' });

export function BudgetsPage() {
  const { budgets, transactions, categories, upsertBudget, deleteBudget } = useFinanceStore();
  const [form, setForm] = useState<Budget>(empty());
  const [editing, setEditing] = useState(false);
  const spent = (budget: Budget) => transactions.filter((transaction)=>transaction.type==='Despesa' && transaction.category===budget.category && transaction.date.startsWith(budget.month)).reduce((total, transaction)=>total.plus(transaction.value), new Decimal(0)).toFixed(2);
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    const limit = parseCurrencyInput(form.limit);
    if (!form.month || !form.category || !limit) return alert('Informe mês, categoria e limite.');
    try {
      await upsertBudget({ ...form, limit });
      alert(editing ? 'Orçamento editado com sucesso.' : 'Orçamento criado com sucesso.');
      setForm(empty());
      setEditing(false);
    } catch (error) {
      alert(error instanceof Error ? error.message : 'Erro ao salvar orçamento.');
    }
  };
  const remove = async (budget: Budget) => {
    if (!window.confirm(`Excluir orçamento de ${budget.category} em ${budget.month}?`)) return;
    await deleteBudget(budget.id);
    alert('Orçamento excluído com sucesso.');
  };
  return <div className="space-y-6"><h1 className="text-3xl font-black">Orçamento mensal</h1><form onSubmit={save} className="card grid gap-3 p-5 md:grid-cols-5"><input className="input" type="month" value={form.month} onChange={(event)=>setForm({...form,month:event.target.value})}/><select className="input md:col-span-2" value={form.category} onChange={(event)=>setForm({...form,category:event.target.value})}>{categories.map((category)=><option key={category.id}>{category.name}</option>)}</select><input className="input" placeholder="Limite mensal" value={form.limit} onChange={(event)=>setForm({...form,limit:event.target.value})}/><div className="flex gap-2"><button className="btn btn-primary" type="submit">{editing?'Salvar':'Criar'}</button>{editing && <button className="btn btn-secondary" type="button" onClick={()=>{setForm(empty());setEditing(false);}}>Cancelar</button>}</div></form><section className="card p-5"><p className="mb-4 text-slate-600 dark:text-slate-300">Limites por categoria com alertas de 80% e 100%.</p><div className="grid gap-3">{budgets.map((budget)=>{ const used=calculateBudgetUsage(budget,transactions); const tone=used>100?'bg-rose-600':used>80?'bg-amber-500':'bg-teal-600'; return <div key={budget.id} className="rounded-2xl bg-slate-50 p-4 dark:bg-slate-900"><div className="flex flex-wrap justify-between gap-3"><div><b>{budget.category}</b><p className="text-sm text-slate-600 dark:text-slate-300">{budget.month} • gasto {formatCurrency(spent(budget))} de {formatCurrency(budget.limit)}</p></div><b className={used>100?'text-rose-600':used>80?'text-amber-600':'text-teal-600'}>{used.toFixed(0)}%</b></div><div className="mt-2 h-3 rounded-full bg-slate-200"><div className={`h-3 rounded-full ${tone}`} style={{width:`${Math.min(100,used)}%`}}/></div><div className="mt-3 flex gap-2"><button className="btn btn-secondary" onClick={()=>{setForm(budget);setEditing(true);}}>Editar</button><button className="btn btn-danger" onClick={()=>remove(budget)}>Excluir</button></div></div>; })}</div>{budgets.length===0 && <p className="text-sm text-slate-600 dark:text-slate-300">Nenhum orçamento cadastrado.</p>}</section></div>;
}
