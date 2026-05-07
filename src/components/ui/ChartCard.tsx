import type { ReactNode } from 'react';
export function ChartCard({ title, subtitle, children }: { title: string; subtitle?: string; children: ReactNode }) { return <section className="card p-5"><div className="mb-5"><h3 className="text-lg font-black">{title}</h3>{subtitle && <p className="text-sm text-slate-500">{subtitle}</p>}</div>{children}</section>; }
