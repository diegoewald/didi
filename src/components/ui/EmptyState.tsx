import { Inbox } from 'lucide-react';

export function EmptyState({ title, description }: { title: string; description: string }) {
  return (
    <div className="rounded-[1.75rem] border border-dashed border-teal-300/70 bg-gradient-to-br from-white/70 to-teal-50/70 p-10 text-center shadow-inner dark:border-teal-800/70 dark:from-slate-950/40 dark:to-teal-950/30">
      <div className="mx-auto grid h-16 w-16 place-items-center rounded-3xl bg-white text-teal-600 shadow-lg dark:bg-slate-900 dark:text-teal-300">
        <Inbox size={34} />
      </div>
      <h3 className="mt-4 text-lg font-black tracking-[-.02em]">{title}</h3>
      <p className="mx-auto mt-2 max-w-md text-sm font-medium leading-6 text-slate-600 dark:text-slate-300">{description}</p>
    </div>
  );
}
