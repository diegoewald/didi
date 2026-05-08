import { useState } from 'react';
import type { CreditCard } from '../../types';
import { creditCardInvoice } from '../../lib/calculations/finance';
import { parseCurrencyInput, formatCurrency } from '../../lib/formatters/formatters';
import { useFinanceStore } from '../transactions/store';

const empty = (): CreditCard => ({ id: crypto.randomUUID(), name: '', limit: '', bestPurchaseDay: 1, closingDay: 1, dueDay: 10, accountName: 'Cartão Nubank', color: '#7c3aed' });
const clampDay = (value: number) => Math.min(31, Math.max(1, Number(value || 1)));

export function CreditCardsPage() {
  const { creditCards, transactions, accounts, upsertCreditCard, deleteCreditCard } = useFinanceStore();
  const [form, setForm] = useState<CreditCard>(empty());
  const [editing, setEditing] = useState(false);
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    const limit = parseCurrencyInput(form.limit);
    if (!form.name.trim() || !limit || !form.accountName.trim()) return alert('Informe nome, limite e conta associada.');
    await upsertCreditCard({ ...form, name: form.name.trim(), limit, bestPurchaseDay: clampDay(form.bestPurchaseDay), closingDay: clampDay(form.closingDay), dueDay: clampDay(form.dueDay) });
    alert(editing ? 'Cartão editado com sucesso.' : 'Cartão cadastrado com sucesso.');
    setForm(empty());
    setEditing(false);
  };
  const remove = async (card: CreditCard) => {
    if (!window.confirm(`Excluir o cartão "${card.name}"?`)) return;
    await deleteCreditCard(card.id);
    alert('Cartão excluído com sucesso.');
  };
  return <div className="space-y-6"><h1 className="text-3xl font-black">Cartões de crédito</h1><form onSubmit={save} className="card grid gap-3 p-5 md:grid-cols-6"><input className="input md:col-span-2" placeholder="Nome do cartão" value={form.name} onChange={(event)=>setForm({...form,name:event.target.value})}/><input className="input" placeholder="Limite" value={form.limit} onChange={(event)=>setForm({...form,limit:event.target.value})}/><input className="input" type="number" min={1} max={31} value={form.bestPurchaseDay} onChange={(event)=>setForm({...form,bestPurchaseDay:Number(event.target.value)})}/><input className="input" type="number" min={1} max={31} value={form.closingDay} onChange={(event)=>setForm({...form,closingDay:Number(event.target.value)})}/><input className="input" type="number" min={1} max={31} value={form.dueDay} onChange={(event)=>setForm({...form,dueDay:Number(event.target.value)})}/><select className="input md:col-span-2" value={form.accountName} onChange={(event)=>setForm({...form,accountName:event.target.value})}>{accounts.map((account)=><option key={account.id}>{account.name}</option>)}</select><input className="input" type="color" value={form.color} onChange={(event)=>setForm({...form,color:event.target.value})}/><div className="flex gap-2 md:col-span-3"><button className="btn btn-primary" type="submit">{editing?'Salvar cartão':'Adicionar cartão'}</button>{editing && <button className="btn btn-secondary" type="button" onClick={()=>{setForm(empty());setEditing(false);}}>Cancelar</button>}</div></form><div className="grid gap-4 md:grid-cols-2">{creditCards.map((card)=>{ const invoice=creditCardInvoice(card,transactions); return <section key={card.id} className="card p-5"><div className="flex justify-between"><h2 className="text-xl font-black">{card.name}</h2><span className="badge bg-purple-100 text-purple-800 dark:bg-purple-950 dark:text-purple-100 dark:border-purple-800">vence dia {card.dueDay}</span></div><p className="mt-5 text-sm text-slate-600 dark:text-slate-300">Fatura atual</p><h3 className="text-3xl font-black">{formatCurrency(invoice.current)}</h3><div className="mt-4 h-3 rounded-full bg-slate-100"><div className="h-3 rounded-full" style={{width:`${Math.min(100,invoice.usedLimitPercent)}%`, background: card.color}}/></div><p className="mt-2 text-sm text-slate-600 dark:text-slate-300">Limite {formatCurrency(card.limit)} • fechamento dia {card.closingDay} • melhor compra dia {card.bestPurchaseDay} • conta {card.accountName}</p><div className="mt-4 flex flex-wrap gap-2"><button className="btn btn-secondary" onClick={()=>{setForm(card);setEditing(true);}}>Editar</button><button className="btn btn-danger" onClick={()=>remove(card)}>Excluir</button><span className="badge">Marcar fatura como paga: em breve</span></div></section>; })}</div></div>;
}
