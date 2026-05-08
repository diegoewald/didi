import { useState } from 'react';
import type { Category } from '../../types';
import { useFinanceStore } from '../transactions/store';

const empty = (): Category => ({ id: crypto.randomUUID(), name: '', color: '#14b8a6', icon: 'Tag', subcategories: [] });

export function CategoriesPage() {
  const { categories, transactions, upsertCategory, deleteCategory } = useFinanceStore();
  const [form, setForm] = useState<Category>(empty());
  const [editing, setEditing] = useState(false);
  const save = async (event: React.FormEvent) => {
    event.preventDefault();
    if (!form.name.trim()) return alert('Informe o nome da categoria.');
    await upsertCategory({ ...form, name: form.name.trim() });
    alert(editing ? 'Categoria editada com sucesso.' : 'Categoria criada com sucesso.');
    setForm(empty());
    setEditing(false);
  };
  const remove = async (category: Category) => {
    const hasTransactions = transactions.some((transaction) => transaction.category === category.name);
    const reclassifyTo = hasTransactions ? window.prompt('Há lançamentos nesta categoria. Digite a categoria para reclassificar:', 'Outros') : 'Outros';
    if (hasTransactions && !reclassifyTo) return;
    if (!window.confirm(`Excluir a categoria "${category.name}"?`)) return;
    await deleteCategory(category.id, reclassifyTo ?? 'Outros');
    alert('Categoria excluída com sucesso.');
  };
  return <div className="space-y-6"><h1 className="text-3xl font-black">Categorias</h1><form onSubmit={save} className="card grid gap-3 p-5 md:grid-cols-5"><input className="input md:col-span-2" value={form.name} onChange={(event)=>setForm({...form,name:event.target.value})} placeholder="Nome da categoria"/><input className="input" type="color" value={form.color} onChange={(event)=>setForm({...form,color:event.target.value})}/><input className="input" value={form.icon} onChange={(event)=>setForm({...form,icon:event.target.value})} placeholder="Ícone"/><div className="flex gap-2"><button className="btn btn-primary" type="submit">{editing?'Salvar':'Criar'}</button>{editing && <button className="btn btn-secondary" type="button" onClick={()=>{setForm(empty());setEditing(false);}}>Cancelar</button>}</div></form><section className="card p-5"><div className="grid gap-3 md:grid-cols-3">{categories.map((category)=><div key={category.id} className="rounded-3xl border border-slate-200 p-4 dark:border-slate-800"><div className="flex items-center justify-between"><b>{category.name}</b><span className="h-5 w-5 rounded-full" style={{background:category.color}}/></div><p className="mt-2 text-sm text-slate-600 dark:text-slate-300">Subcategorias: {category.subcategories.length || 'adicione conforme necessário'}</p><div className="mt-3 flex gap-2"><button className="btn btn-secondary" onClick={()=>{setForm(category);setEditing(true);}}>Editar</button><button className="btn btn-danger" onClick={()=>remove(category)}>Excluir</button></div></div>)}</div></section></div>;
}
