import { routes } from '../../app/routes';

export function MobileNav({ current, navigate }: { current: string; navigate: (path: string) => void }) {
  const visible = routes.slice(0, 5);
  return (
    <nav className="fixed bottom-0 left-0 right-0 z-40 grid grid-cols-5 border-t border-slate-200/80 bg-white/92 px-2 pb-3 pt-2 shadow-[0_-18px_50px_rgba(15,23,42,.10)] backdrop-blur-2xl dark:border-slate-800/80 dark:bg-slate-950/92 lg:hidden">
      {visible.map((route) => {
        const active = current === route.path;
        return (
          <button
            key={route.path}
            aria-label={route.label}
            onClick={() => navigate(route.path)}
            className={`rounded-2xl px-1 py-2 text-[.68rem] font-black transition-all ${active ? 'bg-slate-950 text-white shadow-lg dark:bg-white dark:text-slate-950' : 'text-slate-500 hover:bg-slate-100 dark:text-slate-400 dark:hover:bg-slate-900'}`}
          >
            <route.icon className="mx-auto mb-1" size={19} />
            <span className="block truncate">{route.label.split(' ')[0]}</span>
          </button>
        );
      })}
    </nav>
  );
}
