import { useState } from 'react';
import { Menu, X } from 'lucide-react';
import { routes } from '../../app/routes';

export function MobileNav({ current, navigate }: { current: string; navigate: (path: string) => void }) {
  const [open, setOpen] = useState(false);
  const primary = routes.slice(0, 4);
  const currentRoute = routes.find((route) => route.path === current);

  const go = (path: string) => {
    navigate(path);
    setOpen(false);
  };

  return (
    <>
      {open && <button className="fixed inset-0 z-40 bg-slate-950/45 backdrop-blur-sm lg:hidden" aria-label="Fechar menu" onClick={() => setOpen(false)} />}
      <aside className={`fixed bottom-0 left-0 right-0 z-50 max-h-[82vh] rounded-t-[2rem] border border-slate-200 bg-white p-4 shadow-2xl transition-transform duration-200 dark:border-slate-800 dark:bg-slate-950 lg:hidden ${open ? 'translate-y-0' : 'translate-y-full'}`}>
        <div className="mb-4 flex items-center justify-between gap-3">
          <div>
            <p className="text-xs font-black uppercase tracking-[.18em] text-teal-700 dark:text-teal-300">Menu V1.5.6</p>
            <h2 className="text-xl font-black">Todas as telas</h2>
          </div>
          <button className="btn btn-secondary !min-h-11 !px-3" aria-label="Fechar menu" onClick={() => setOpen(false)}><X size={20} /></button>
        </div>
        <nav className="grid max-h-[58vh] grid-cols-2 gap-2 overflow-auto pb-2 scrollbar min-[390px]:grid-cols-3">
          {routes.map((route) => {
            const active = current === route.path;
            return (
              <button
                key={route.path}
                aria-label={`Abrir ${route.label}`}
                onClick={() => go(route.path)}
                className={`min-h-16 rounded-2xl border px-3 py-3 text-left text-xs font-black transition ${active ? 'border-teal-500 bg-teal-50 text-teal-900 shadow-sm dark:bg-teal-950 dark:text-teal-100' : 'border-slate-200 bg-slate-50 text-slate-700 hover:border-teal-300 hover:bg-white dark:border-slate-800 dark:bg-slate-900 dark:text-slate-200 dark:hover:border-teal-700'}`}
              >
                <route.icon className="mb-1" size={18} />
                <span className="block leading-tight">{route.label}</span>
              </button>
            );
          })}
        </nav>
      </aside>

      <nav className="fixed bottom-0 left-0 right-0 z-40 grid grid-cols-5 gap-1 border-t border-slate-200/80 bg-white/95 px-2 pb-[calc(.55rem+env(safe-area-inset-bottom))] pt-2 shadow-[0_-18px_50px_rgba(15,23,42,.10)] backdrop-blur-2xl dark:border-slate-800/80 dark:bg-slate-950/95 lg:hidden">
        {primary.map((route) => {
          const active = current === route.path;
          return (
            <button
              key={route.path}
              aria-label={route.label}
              onClick={() => go(route.path)}
              className={`min-h-14 rounded-2xl px-1 py-2 text-[.67rem] font-black transition-all ${active ? 'bg-slate-950 text-white shadow-lg dark:bg-white dark:text-slate-950' : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-900'}`}
            >
              <route.icon className="mx-auto mb-1" size={19} />
              <span className="block truncate">{route.label.split(' ')[0]}</span>
            </button>
          );
        })}
        <button
          aria-label="Abrir todas as telas"
          onClick={() => setOpen(true)}
          className={`min-h-14 rounded-2xl px-1 py-2 text-[.67rem] font-black transition-all ${open || !primary.some((route) => route.path === current) ? 'bg-slate-950 text-white shadow-lg dark:bg-white dark:text-slate-950' : 'text-slate-700 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-900'}`}
        >
          <Menu className="mx-auto mb-1" size={19} />
          <span className="block truncate">{currentRoute && !primary.some((route) => route.path === current) ? currentRoute.label.split(' ')[0] : 'Mais'}</span>
        </button>
      </nav>
    </>
  );
}
