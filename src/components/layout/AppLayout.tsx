import type { ReactNode } from 'react';
import { LockKeyhole, Sparkles } from 'lucide-react';
import { Sidebar } from './Sidebar';
import { MobileNav } from './MobileNav';
import { ThemeToggle } from '../ui/ThemeToggle';

export function AppLayout({
  current,
  navigate,
  dark,
  toggleDark,
  children,
}: {
  current: string;
  navigate: (path: string) => void;
  dark: boolean;
  toggleDark: () => void;
  children: ReactNode;
}) {
  return (
    <div className="relative min-h-screen overflow-hidden app-surface pb-24 lg:pb-0">
      <div className="premium-glow pointer-events-none fixed -left-32 top-8 h-80 w-80 rounded-full bg-teal-300/30 blur-3xl dark:bg-teal-500/15" />
      <div className="premium-glow pointer-events-none fixed -right-36 top-32 h-96 w-96 rounded-full bg-indigo-300/30 blur-3xl dark:bg-indigo-500/20" />
      <Sidebar current={current} navigate={navigate} />
      <div className="relative lg:pl-72">
        <header className="sticky top-0 z-20 border-b border-slate-200/70 dark:border-slate-800/80 px-3 py-3 glass sm:px-4 lg:px-8">
          <div className="mx-auto flex max-w-7xl items-center justify-between gap-3">
            <div className="min-w-0">
              <div className="flex items-center gap-2 text-xs font-black uppercase tracking-[.25em] text-teal-700 dark:text-teal-300">
                <Sparkles size={14} /> FinançasPro V1.5.6
              </div>
              <h2 className="truncate text-lg font-black tracking-[-.03em] text-slate-950 dark:text-white sm:text-2xl">
                Controle financeiro premium
              </h2>
            </div>
            <div className="flex shrink-0 items-center gap-2">
              <div className="hidden items-center gap-2 rounded-full border border-emerald-200/80 bg-emerald-50/90 px-3 py-2 text-xs font-black text-emerald-800 shadow-sm dark:border-emerald-800/60 dark:bg-emerald-950/70 dark:text-emerald-100 sm:flex">
                <LockKeyhole size={15} /> Offline e privado
              </div>
              <button className="btn btn-secondary !px-3" aria-label="Alertas em breve" title="Alertas em breve" disabled>
                Alertas em breve
              </button>
              <ThemeToggle dark={dark} onToggle={toggleDark} />
            </div>
          </div>
        </header>
        <main className="mx-auto max-w-7xl px-3 py-4 sm:px-5 sm:py-6 lg:p-8">{children}</main>
      </div>
      <MobileNav current={current} navigate={navigate} />
    </div>
  );
}
