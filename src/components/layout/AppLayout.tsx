import type { ReactNode } from 'react';
import { Bell, LockKeyhole } from 'lucide-react';
import { Sidebar } from './Sidebar';
import { MobileNav } from './MobileNav';
import { ThemeToggle } from '../ui/ThemeToggle';

export function AppLayout({ current, navigate, dark, toggleDark, children }: { current: string; navigate: (path: string) => void; dark: boolean; toggleDark: () => void; children: ReactNode }) {
  return <div className="min-h-screen bg-[radial-gradient(circle_at_top_left,#ccfbf1,transparent_35%),radial-gradient(circle_at_top_right,#dbeafe,transparent_30%)] pb-24 text-slate-950 dark:bg-[radial-gradient(circle_at_top_left,#0f766e33,transparent_35%),radial-gradient(circle_at_top_right,#3730a333,transparent_30%)] dark:text-slate-100 lg:pb-0"><Sidebar current={current} navigate={navigate}/><div className="lg:pl-72"><header className="sticky top-0 z-20 flex items-center justify-between gap-3 border-b border-white/40 p-4 glass lg:px-8"><div><p className="text-xs font-black uppercase tracking-[.25em] text-teal-600">DIDI Forensics</p><h2 className="text-xl font-black">Análise técnica de imagens geradas por IA</h2></div><div className="flex items-center gap-2"><div className="hidden items-center gap-2 rounded-full bg-emerald-100 px-3 py-2 text-xs font-black text-emerald-800 dark:bg-emerald-950 dark:text-emerald-200 sm:flex"><LockKeyhole size={15}/> Processamento controlado</div><button className="btn btn-secondary" aria-label="Alertas"><Bell size={18}/></button><ThemeToggle dark={dark} onToggle={toggleDark}/></div></header><main className="mx-auto max-w-7xl p-4 lg:p-8">{children}</main></div><MobileNav current={current} navigate={navigate}/></div>;
}
