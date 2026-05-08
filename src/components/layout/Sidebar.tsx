import { ShieldCheck } from 'lucide-react';
import { routes } from '../../app/routes';

export function Sidebar({ current, navigate }: { current: string; navigate: (path: string) => void }) {
  return (
    <aside className="fixed left-0 top-0 z-30 hidden h-screen w-72 flex-col border-r border-white/60 bg-white/90 p-5 shadow-[18px_0_60px_rgba(15,23,42,.06)] backdrop-blur-2xl dark:border-slate-800/70 dark:bg-slate-950/90 lg:flex">
      <button onClick={() => navigate('/dashboard')} className="group flex items-center gap-3 rounded-3xl p-2 text-left transition hover:bg-slate-100/80 dark:hover:bg-slate-900">
        <div className="rounded-2xl bg-gradient-to-br from-teal-500 via-cyan-500 to-indigo-600 p-3 text-white shadow-xl shadow-teal-500/20 transition group-hover:scale-105">
          <ShieldCheck />
        </div>
        <div>
          <h1 className="text-xl font-black tracking-[-.04em]">FinançasPro</h1>
          <p className="text-xs font-extrabold uppercase tracking-[.18em] text-teal-700 dark:text-teal-300">local-first</p>
        </div>
      </button>
      <nav className="mt-7 space-y-1.5 overflow-auto pb-6 pr-1 scrollbar">
        {routes.map((route) => {
          const active = current === route.path;
          return (
            <button
              key={route.path}
              onClick={() => navigate(route.path)}
              className={`group flex w-full items-center gap-3 rounded-2xl px-4 py-3 text-left text-sm font-extrabold transition-all duration-200 ${
                active
                  ? 'bg-slate-950 text-white shadow-xl shadow-slate-900/20 dark:bg-white dark:text-slate-950 dark:shadow-white/10'
                  : 'text-slate-600 hover:bg-white hover:text-slate-950 hover:shadow-md dark:text-slate-300 dark:hover:bg-slate-900 dark:hover:text-white'
              }`}
            >
              <span className={`rounded-xl p-2 transition ${active ? 'bg-white/20 dark:bg-slate-950/10' : 'bg-slate-100 text-slate-600 dark:text-slate-300 group-hover:bg-teal-50 group-hover:text-teal-700 dark:bg-slate-900 dark:text-slate-400 dark:group-hover:bg-teal-950 dark:group-hover:text-teal-200'}`}>
                <route.icon size={17} />
              </span>
              <span className="truncate">{route.label}</span>
            </button>
          );
        })}
      </nav>
      <div className="mt-auto rounded-3xl border border-teal-200/70 bg-gradient-to-br from-teal-50 to-cyan-50 p-4 text-sm text-teal-950 shadow-inner dark:border-teal-900/70 dark:from-teal-950/70 dark:to-slate-900 dark:text-teal-50">
        <b className="mb-1 block">Privacidade protegida</b>
        Seus dados ficam no IndexedDB deste navegador. Nenhuma API externa obrigatória é usada.
      </div>
    </aside>
  );
}
