import { ImportWizard } from '../../components/import/ImportWizard';
import { useFinanceStore } from '../transactions/store';
export function ImportPage() { const { transactions, addTransactions } = useFinanceStore(); return <div className="space-y-6"><div><h1 className="text-3xl font-black">Importar planilha</h1><p className="text-slate-500">Aceita Excel e CSV, detecta colunas parecidas, valida erros e mostra prévia antes de salvar.</p></div><ImportWizard existing={transactions} onConfirm={addTransactions}/></div>; }
