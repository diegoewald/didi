import { useState } from 'react';
import { useAuthStore } from './authStore';

export function AuthPage() {
  const auth = useAuthStore();
  const [mode, setMode] = useState<'login' | 'signup'>('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [busy, setBusy] = useState(false);
  const [message, setMessage] = useState<string>();

  const submit = async (event: React.FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setMessage(undefined);
    try {
      if (mode === 'login') await auth.signIn(email, password);
      else await auth.signUp(email, password);
      setMessage(mode === 'login' ? 'Login concluído. Seus dados podem ser sincronizados.' : 'Conta criada. Confira seu email se o Supabase exigir confirmação.');
    } catch (error) {
      setMessage(error instanceof Error ? error.message : 'Não foi possível autenticar.');
    } finally {
      setBusy(false);
    }
  };

  if (!auth.configured) {
    return <section className="card space-y-4 p-5"><h1 className="text-3xl font-black">Entrar para sincronizar</h1><p className="text-sm text-slate-600 dark:text-slate-300">Modo local ativo. Configure VITE_SUPABASE_URL e VITE_SUPABASE_ANON_KEY para habilitar login e sincronização online.</p></section>;
  }

  return <section className="mx-auto max-w-xl space-y-5"><div><p className="text-sm font-black uppercase tracking-[.2em] text-teal-700 dark:text-teal-300">FinançasPro Online</p><h1 className="text-3xl font-black">{mode === 'login' ? 'Entrar para sincronizar' : 'Criar conta'}</h1><p className="mt-2 text-sm text-slate-600 dark:text-slate-300">Use email e senha do Supabase Auth. Nunca informe chaves privilegiadas no app.</p></div><form onSubmit={submit} className="card space-y-4 p-5"><label><span className="label">Email</span><input className="input" type="email" value={email} onChange={(event)=>setEmail(event.target.value)} required /></label><label><span className="label">Senha</span><input className="input" type="password" minLength={6} value={password} onChange={(event)=>setPassword(event.target.value)} required /></label>{message && <div className="rounded-2xl p-3 text-sm font-bold alert-warning">{message}</div>}<button className="btn btn-primary w-full" disabled={busy}>{busy ? 'Aguarde...' : mode === 'login' ? 'Entrar' : 'Criar conta'}</button><button type="button" className="btn btn-secondary w-full" onClick={()=>setMode(mode === 'login' ? 'signup' : 'login')}>{mode === 'login' ? 'Ainda não tenho conta' : 'Já tenho conta'}</button></form></section>;
}
