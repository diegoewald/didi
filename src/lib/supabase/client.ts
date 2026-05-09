export interface SupabaseSession {
  access_token: string;
  refresh_token?: string;
  expires_at?: number;
  user: { id: string; email?: string };
}

interface AuthResponse {
  access_token?: string;
  refresh_token?: string;
  expires_in?: number;
  user?: { id: string; email?: string };
  error?: string;
  error_description?: string;
  msg?: string;
}

export interface SupabasePublicConfig {
  url: string;
  anonKey: string;
}

const sessionKey = 'financaspro-supabase-session';
export const supabaseNotConfiguredMessage = 'Modo local ativo. Configure o Supabase para sincronização online.';

export function resolveSupabaseConfig(env: { VITE_SUPABASE_URL?: string; VITE_SUPABASE_ANON_KEY?: string }): SupabasePublicConfig {
  return {
    url: (env.VITE_SUPABASE_URL ?? '').trim().replace(/\/$/, ''),
    anonKey: (env.VITE_SUPABASE_ANON_KEY ?? '').trim(),
  };
}

export const supabaseConfig = resolveSupabaseConfig({ VITE_SUPABASE_URL: import.meta.env.VITE_SUPABASE_URL, VITE_SUPABASE_ANON_KEY: import.meta.env.VITE_SUPABASE_ANON_KEY });

export function isSupabaseConfigured(config = supabaseConfig): boolean {
  return Boolean(config.url && config.anonKey);
}

export function getStoredSession(): SupabaseSession | null {
  try {
    const raw = localStorage.getItem(sessionKey);
    if (!raw) return null;
    const parsed = JSON.parse(raw) as SupabaseSession;
    return parsed.access_token && parsed.user?.id ? parsed : null;
  } catch (error) {
    console.warn('Sessão online inválida removida.', error);
    localStorage.removeItem(sessionKey);
    return null;
  }
}

export function storeSession(session: SupabaseSession | null): void {
  if (!session) localStorage.removeItem(sessionKey);
  else localStorage.setItem(sessionKey, JSON.stringify(session));
}

function authHeaders(token?: string): HeadersInit {
  return {
    apikey: supabaseConfig.anonKey,
    Authorization: `Bearer ${token ?? supabaseConfig.anonKey}`,
    'Content-Type': 'application/json',
  };
}

function toFriendlyAuthError(payload: AuthResponse): Error {
  return new Error(payload.error_description || payload.msg || payload.error || 'Não foi possível autenticar. Confira email e senha.');
}

function normalizeSession(payload: AuthResponse): SupabaseSession | null {
  if (!payload.access_token || !payload.user?.id) return null;
  return {
    access_token: payload.access_token,
    refresh_token: payload.refresh_token,
    expires_at: payload.expires_in ? Math.floor(Date.now() / 1000) + payload.expires_in : undefined,
    user: { id: payload.user.id, email: payload.user.email },
  };
}

export async function signInWithPassword(email: string, password: string): Promise<SupabaseSession> {
  if (!isSupabaseConfigured()) throw new Error(supabaseNotConfiguredMessage);
  const response = await fetch(`${supabaseConfig.url}/auth/v1/token?grant_type=password`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify({ email, password }),
  });
  const payload = (await response.json()) as AuthResponse;
  if (!response.ok) throw toFriendlyAuthError(payload);
  const session = normalizeSession(payload);
  if (!session) throw new Error('Sessão não retornada. Confirme seu email e tente entrar novamente.');
  storeSession(session);
  return session;
}

export async function signUpWithPassword(email: string, password: string): Promise<SupabaseSession | null> {
  if (!isSupabaseConfigured()) throw new Error(supabaseNotConfiguredMessage);
  const response = await fetch(`${supabaseConfig.url}/auth/v1/signup`, {
    method: 'POST',
    headers: authHeaders(),
    body: JSON.stringify({ email, password }),
  });
  const payload = (await response.json()) as AuthResponse;
  if (!response.ok) throw toFriendlyAuthError(payload);
  const session = normalizeSession(payload);
  storeSession(session);
  return session;
}

export async function signOut(session: SupabaseSession | null): Promise<void> {
  if (isSupabaseConfigured() && session?.access_token) {
    try {
      await fetch(`${supabaseConfig.url}/auth/v1/logout`, { method: 'POST', headers: authHeaders(session.access_token) });
    } catch (error) {
      console.warn('Não foi possível encerrar sessão no Supabase, removendo sessão local.', error);
    }
  }
  storeSession(null);
}

export async function supabaseFetch<T>(path: string, init: RequestInit = {}, session = getStoredSession()): Promise<T> {
  if (!isSupabaseConfigured()) throw new Error(supabaseNotConfiguredMessage);
  if (!session?.access_token) throw new Error('Sessão expirada. Entre novamente.');
  const response = await fetch(`${supabaseConfig.url}${path}`, {
    ...init,
    headers: { ...authHeaders(session.access_token), Prefer: 'return=representation', ...(init.headers ?? {}) },
  });
  const text = await response.text();
  const data = text ? (JSON.parse(text) as T) : ([] as T);
  if (!response.ok) {
    console.error('Erro Supabase:', response.status, data);
    throw new Error('Não foi possível sincronizar agora. Seus dados continuam salvos neste dispositivo.');
  }
  return data;
}
