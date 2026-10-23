'use client';

import { useMemo, useState } from 'react';
import { AlertTriangle, BrainCircuit, CheckCircle2, Loader2, Microscope, ShieldAlert, Sparkles, UploadCloud } from 'lucide-react';
import { Area, AreaChart, Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts';

type Severity = 'info' | 'low' | 'medium' | 'high' | 'critical';
type Evidence = { title: string; severity: Severity; description: string; score_impact: number };
type HistogramPoint = { bin: number; r: number; g: number; b: number };
type Report = {
  filename: string;
  mime_type: string;
  width: number;
  height: number;
  suspicion_score: number;
  category: 'Inconclusivo' | 'Suspeita leve' | 'Suspeita moderada' | 'Muito provável IA' | 'Evidência técnica forte de IA';
  conclusion: string;
  evidence: Evidence[];
  metadata: Record<string, unknown>;
  jpeg: Record<string, unknown>;
  compression: Record<string, unknown>;
  entropy: Record<string, unknown>;
  texture: Record<string, unknown>;
  channels: Record<string, unknown>;
  dct: Record<string, unknown>;
  objects: Record<string, unknown>;
  histograms: HistogramPoint[];
  maps: { ela: string; noise: string; edges: string; sharpness: string; fft: string };
};

declare const process: { env: Record<string, string | undefined> } | undefined;
const configuredApiUrl = typeof process !== 'undefined' ? process.env.NEXT_PUBLIC_FORENSICS_API_URL : undefined;
const API_URL = configuredApiUrl ?? 'http://localhost:8000';
const categories: Report['category'][] = ['Inconclusivo', 'Suspeita leve', 'Suspeita moderada', 'Muito provável IA', 'Evidência técnica forte de IA'];

function severityClass(severity: Severity) {
  return {
    info: 'bg-sky-100 text-sky-800 dark:bg-sky-950 dark:text-sky-200',
    low: 'bg-lime-100 text-lime-800 dark:bg-lime-950 dark:text-lime-200',
    medium: 'bg-amber-100 text-amber-900 dark:bg-amber-950 dark:text-amber-100',
    high: 'bg-orange-100 text-orange-900 dark:bg-orange-950 dark:text-orange-100',
    critical: 'bg-rose-100 text-rose-900 dark:bg-rose-950 dark:text-rose-100',
  }[severity];
}

function compact(value: unknown): string {
  if (value === null || value === undefined) return '—';
  if (typeof value === 'object') return JSON.stringify(value, null, 2);
  return String(value);
}

function MetricCard({ label, value, detail }: { label: string; value: string | number; detail: string }) {
  return <div className="card p-5"><p className="text-xs font-black uppercase tracking-[.2em] text-teal-600">{label}</p><p className="mt-2 text-3xl font-black">{value}</p><p className="mt-2 text-sm font-semibold text-slate-500 dark:text-slate-400">{detail}</p></div>;
}

function JsonPanel({ title, data }: { title: string; data: Record<string, unknown> }) {
  const rows = Object.entries(data).slice(0, 10);
  return <div className="card p-5"><h3 className="text-lg font-black">{title}</h3><div className="mt-4 space-y-3">{rows.map(([key, value]) => <div key={key} className="rounded-2xl bg-slate-50 p-3 dark:bg-slate-900"><p className="text-xs font-black uppercase text-slate-500">{key}</p><pre className="mt-1 max-h-40 overflow-auto whitespace-pre-wrap break-words text-xs font-semibold">{compact(value)}</pre></div>)}</div></div>;
}

function MapCard({ title, src, description }: { title: string; src: string; description: string }) {
  return <div className="card overflow-hidden"><img src={src} alt={title} className="h-64 w-full object-cover"/><div className="p-4"><h3 className="font-black">{title}</h3><p className="mt-1 text-sm font-semibold text-slate-500 dark:text-slate-400">{description}</p></div></div>;
}

export function ForensicsPage() {
  const [file, setFile] = useState<File | null>(null);
  const [preview, setPreview] = useState<string>('');
  const [report, setReport] = useState<Report | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const selectedCategory = useMemo(() => report ? categories.indexOf(report.category) : -1, [report]);

  function onFileChange(nextFile: File | undefined) {
    setError('');
    setReport(null);
    if (!nextFile) return;
    if (!['image/jpeg', 'image/png', 'image/webp'].includes(nextFile.type)) {
      setError('Formato inválido. Envie JPG, PNG ou WEBP.');
      return;
    }
    if (nextFile.size > 16 * 1024 * 1024) {
      setError('Arquivo maior que 16 MB.');
      return;
    }
    setFile(nextFile);
    setPreview(URL.createObjectURL(nextFile));
  }

  async function analyze() {
    if (!file) {
      setError('Selecione uma imagem antes de analisar.');
      return;
    }
    setLoading(true);
    setError('');
    const form = new FormData();
    form.append('file', file);
    try {
      const response = await fetch(`${API_URL}/api/analyze`, { method: 'POST', body: form });
      if (!response.ok) {
        const detail = await response.json().catch(() => ({ detail: 'Falha ao analisar imagem.' })) as { detail?: string };
        throw new Error(detail.detail ?? 'Falha ao analisar imagem.');
      }
      setReport(await response.json() as Report);
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Erro inesperado ao conectar ao backend.');
    } finally {
      setLoading(false);
    }
  }

  return <div className="space-y-8 animate-rise">
    <section className="grid gap-6 lg:grid-cols-[1.1fr_.9fr]">
      <div className="card overflow-hidden p-8">
        <div className="flex flex-wrap items-center gap-3"><span className="badge bg-teal-100 text-teal-800 dark:bg-teal-950 dark:text-teal-100">Análise probabilística</span><span className="badge bg-indigo-100 text-indigo-800 dark:bg-indigo-950 dark:text-indigo-100">FastAPI + OpenCV</span></div>
        <h1 className="mt-5 text-4xl font-black tracking-tight lg:text-6xl">Laboratório forense para imagens suspeitas de IA</h1>
        <p className="mt-5 max-w-3xl text-lg font-semibold text-slate-600 dark:text-slate-300">Envie uma imagem JPG, PNG ou WEBP e obtenha um relatório técnico com metadados, compressão, ELA, ruído, nitidez, bordas, FFT, DCT, entropia, textura e correlações cromáticas. O sistema nunca afirma 100% de certeza.</p>
        <div className="mt-6 grid gap-3 sm:grid-cols-3">{['Sem certeza absoluta', 'Relatório técnico', 'Mapas visuais'].map(item => <div key={item} className="flex items-center gap-2 rounded-2xl bg-slate-50 p-3 font-bold dark:bg-slate-900"><CheckCircle2 className="text-teal-500" size={18}/>{item}</div>)}</div>
      </div>
      <div className="card p-6">
        <label className="flex min-h-72 cursor-pointer flex-col items-center justify-center rounded-3xl border-2 border-dashed border-teal-300 bg-teal-50/60 p-6 text-center transition hover:bg-teal-50 dark:border-teal-800 dark:bg-teal-950/30">
          {preview ? <img src={preview} alt="Pré-visualização" className="max-h-56 rounded-2xl object-contain shadow-xl"/> : <><UploadCloud size={54} className="text-teal-600"/><p className="mt-4 text-xl font-black">Solte ou selecione a imagem</p><p className="mt-2 text-sm font-bold text-slate-500">JPG, PNG ou WEBP até 16 MB</p></>}
          <input type="file" accept="image/jpeg,image/png,image/webp" className="hidden" onChange={event => onFileChange(event.target.files?.[0])}/>
        </label>
        <button onClick={analyze} disabled={loading || !file} className="btn btn-primary mt-5 w-full disabled:cursor-not-allowed disabled:opacity-60">{loading ? <Loader2 className="animate-spin"/> : <Microscope/>}{loading ? 'Analisando artefatos...' : 'Gerar relatório forense'}</button>
        {error && <div className="mt-4 rounded-2xl bg-rose-100 p-4 font-bold text-rose-800 dark:bg-rose-950 dark:text-rose-100"><AlertTriangle className="mr-2 inline" size={18}/>{error}</div>}
      </div>
    </section>

    {report && <>
      <section className="grid gap-4 md:grid-cols-4">
        <MetricCard label="Pontuação" value={`${report.suspicion_score}/100`} detail="Escala heurística de suspeita técnica."/>
        <MetricCard label="Categoria" value={report.category} detail="Classificação sem alegação de certeza absoluta."/>
        <MetricCard label="Imagem" value={`${report.width}×${report.height}`} detail={`${report.filename} • ${report.mime_type}`}/>
        <MetricCard label="Evidências" value={report.evidence.length} detail="Indícios ponderados encontrados."/>
      </section>

      <section className="card p-6">
        <div className="flex flex-wrap items-start justify-between gap-4"><div><p className="text-xs font-black uppercase tracking-[.2em] text-teal-600">Conclusão clara</p><h2 className="mt-2 text-2xl font-black">{report.conclusion}</h2></div><ShieldAlert className="text-amber-500" size={38}/></div>
        <div className="mt-6 grid gap-2 md:grid-cols-5">{categories.map((category, index) => <div key={category} className={`rounded-2xl p-3 text-center text-sm font-black ${index <= selectedCategory ? 'bg-teal-600 text-white' : 'bg-slate-100 text-slate-500 dark:bg-slate-900'}`}>{category}</div>)}</div>
      </section>

      <section className="grid gap-6 lg:grid-cols-[.9fr_1.1fr]">
        <div className="card p-6"><h2 className="flex items-center gap-2 text-2xl font-black"><Sparkles className="text-teal-500"/>Evidências encontradas</h2><div className="mt-5 space-y-3">{report.evidence.map(item => <div key={item.title} className="rounded-3xl border border-slate-200 p-4 dark:border-slate-800"><div className="flex items-center justify-between gap-3"><h3 className="font-black">{item.title}</h3><span className={`badge ${severityClass(item.severity)}`}>+{item.score_impact}</span></div><p className="mt-2 text-sm font-semibold text-slate-600 dark:text-slate-300">{item.description}</p></div>)}</div></div>
        <div className="card p-6"><h2 className="text-2xl font-black">Histograma RGB</h2><div className="mt-4 h-80"><ResponsiveContainer width="100%" height="100%"><AreaChart data={report.histograms}><CartesianGrid strokeDasharray="3 3"/><XAxis dataKey="bin"/><YAxis/><Tooltip/><Area type="monotone" dataKey="r" stroke="#ef4444" fill="#ef444455"/><Area type="monotone" dataKey="g" stroke="#22c55e" fill="#22c55e44"/><Area type="monotone" dataKey="b" stroke="#3b82f6" fill="#3b82f644"/></AreaChart></ResponsiveContainer></div></div>
      </section>

      <section className="grid gap-5 md:grid-cols-2 xl:grid-cols-5">
        <MapCard title="Error Level Analysis" src={report.maps.ela} description="Diferenças após recompressão JPEG por região."/>
        <MapCard title="Mapa de ruído" src={report.maps.noise} description="Residual após denoising para inferir padrão de sensor."/>
        <MapCard title="Bordas Sobel/Canny" src={report.maps.edges} description="Contornos e transições de alto gradiente."/>
        <MapCard title="Mapa de nitidez" src={report.maps.sharpness} description="Variância do Laplaciano em blocos locais."/>
        <MapCard title="Espectro FFT" src={report.maps.fft} description="Energia de frequências e periodicidades."/>
      </section>

      <section className="grid gap-6 lg:grid-cols-3">
        <JsonPanel title="Metadados EXIF/XMP/IPTC/ICC" data={report.metadata}/>
        <JsonPanel title="Compressão e JPEG" data={{ ...report.jpeg, ...report.compression }}/>
        <JsonPanel title="Sinais estatísticos" data={{ entropy: report.entropy, texture: report.texture, channels: report.channels, dct: report.dct, objects: report.objects }}/>
      </section>

      <section className="card p-6"><h2 className="flex items-center gap-2 text-2xl font-black"><BrainCircuit className="text-indigo-500"/>Perfil técnico resumido</h2><div className="mt-4 h-72"><ResponsiveContainer width="100%" height="100%"><BarChart data={[{ name: 'Suspeita', value: report.suspicion_score }, { name: 'Confiança restante', value: 100 - report.suspicion_score }]}><CartesianGrid strokeDasharray="3 3"/><XAxis dataKey="name"/><YAxis/><Tooltip/><Bar dataKey="value" fill="#14b8a6" radius={[12, 12, 0, 0]}/></BarChart></ResponsiveContainer></div><p className="mt-4 text-sm font-bold text-slate-500">Use o relatório como triagem técnica. Para decisões críticas, combine com cadeia de custódia, imagem original, múltiplas ferramentas e revisão humana especializada.</p></section>
    </>}
  </div>;
}
