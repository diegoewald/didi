import { routes } from '../../app/routes';

export function MobileNav({ current, navigate }: { current: string; navigate: (path: string) => void }) {
  const visible = routes.slice(0, 4);
  return <nav className="fixed bottom-0 left-0 right-0 z-40 grid grid-cols-4 border-t border-slate-200 bg-white/95 p-2 backdrop-blur-xl dark:border-slate-800 dark:bg-slate-950/95 lg:hidden">{visible.map(route => <button key={route.path} aria-label={route.label} onClick={() => navigate(route.path)} className={`rounded-2xl p-2 text-xs font-bold ${current === route.path ? 'bg-teal-600 text-white' : 'text-slate-500'}`}><route.icon className="mx-auto mb-1" size={19}/>{route.label.split(' ')[0]}</button>)}</nav>;
}
