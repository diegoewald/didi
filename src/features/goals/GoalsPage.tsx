import { useState } from 'react';
import Decimal from 'decimal.js';
import { differenceInMonths, parseISO } from 'date-fns';
import type { Goal } from '../../types';
import { parseCurrencyInput, formatCurrency, formatDateBR } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

const empty = (): Goal => ({ id: crypto.randomUUID(), name: '', targetValue: '', currentValue: '0', targetDate: new Date().toISOString().slice(0, 10), category: 'Emergência', color: '#22c55e' });

export function GoalsPage() {
  const { goals, categories, upsertGoal, deleteGoal } = useFinanceStore();
  const [form, setForm] = useState<Goal>(empty());
  const [editing, setEditing] = useState(false);
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    const targetValue = parseCurrencyInput(form.targetValue);
    const currentValue = parseCurrencyInput(form.currentValue) ?? '0.00';
    if (!form.name.trim() || !targetValue || !form.targetDate) return alert('Preencha nome, valor alvo e data alvo.');
    await upsertGoal({ ...form, name: form.name.trim(), targetValue, currentValue });
    alert(editing ? 'Meta editada com sucesso.' : 'Meta criada com sucesso.');
    setForm(empty());
    setEditing(false);
  };
  const remove = async (goal: Goal) => {
    if (!window.confirm(`Excluir a meta "${goal.name}"?`)) return;
    await deleteGoal(goal.id);
    alert('Meta excluída com sucesso.');
  };
  return <div className="space-y-6"><h1 className="text-3xl font-black">Metas financeiras</h1><form onSubmit={save} className="card grid gap-3 p-5 md:grid-cols-6"><input className="input md:col-span-2" placeholder="Nome da meta" value={form.name} onChange={(event)=>setForm({...form,name:event.target.value})}/><input className="input" placeholder="Valor alvo" value={form.targetValue} onChange={(event)=>setForm({...form,targetValue:event.target.value})}/><input className="input" placeholder="Valor atual" value={form.currentValue} onChange={(event)=>setForm({...form,currentValue:event.target.value})}/><input className="input" type="date" value={form.targetDate} onChange={(event)=>setForm({...form,targetDate:event.target.value})}/><input className="input" type="color" value={form.color} onChange={(event)=>setForm({...form,color:event.target.value})}/><select className="input md:col-span-2" value={form.category} onChange={(event)=>setForm({...form,category:event.target.value})}>{categories.map((category)=><option key={category.id}>{category.name}</option>)}</select><div className="flex gap-2 md:col-span-4"><button className="btn btn-primary" type="submit">{editing?'Salvar alterações':'Criar meta'}</button>{editing && <button className="btn btn-secondary" type="button" onClick={()=>{setForm(empty());setEditing(false);}}>Cancelar</button>}</div></form><div className="grid gap-4 md:grid-cols-3">{goals.map((goal)=>{ const pct=Number(goal.targetValue)>0 ? Number(goal.currentValue)/Number(goal.targetValue)*100 : 0; const months=Math.max(1,differenceInMonths(parseISO(goal.targetDate),new Date())); const monthly=new Decimal(goal.targetValue || 0).minus(goal.currentValue || 0).div(months).toFixed(2); return <section className="card p-5" key={goal.id}><div className="flex justify-between gap-3"><b>{goal.name}</b><span className="h-5 w-5 rounded-full" style={{background:goal.color}}/></div><p className="mt-3 text-2xl font-black">{pct.toFixed(1)}%</p><div className="mt-2 h-3 rounded-full bg-slate-100"><div className="h-3 rounded-full" style={{width:`${Math.min(100,pct)}%`, background: goal.color}}/></div><p className="mt-3 text-sm text-slate-600 dark:text-slate-300">{formatCurrency(goal.currentValue)} de {formatCurrency(goal.targetValue)} até {formatDateBR(goal.targetDate)}</p><p className="mt-2 font-bold">Guardar {formatCurrency(monthly)}/mês</p><div className="mt-4 flex gap-2"><button className="btn btn-secondary" onClick={()=>{setForm(goal);setEditing(true);}}>Editar</button><button className="btn btn-danger" onClick={()=>remove(goal)}>Excluir</button></div></section>; })}</div></div>;
}
