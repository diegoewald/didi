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

const sessionKey = 'financaspro-supabase-session';

export const supabaseConfig = {
  url: (import.meta.env.VITE_SUPABASE_URL as string | undefined)?.replace(/\/$/, '') ?? '',
  anonKey: (import.meta.env.VITE_SUPABASE_ANON_KEY as string | undefined) ?? '',
};

export function isSupabaseConfigured(): boolean {
  return Boolean(supabaseConfig.url && supabaseConfig.anonKey);
}

export function getStoredSession(): SupabaseSession | null {
  try {
    const raw = localStorage.getItem(sessionKey);
    return raw ? (JSON.parse(raw) as SupabaseSession) : null;
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

function normalizeSession(payload: AuthResponse): SupabaseSession {
  if (!payload.access_token || !payload.user?.id) throw toFriendlyAuthError(payload);
  return {
    access_token: payload.access_token,
    refresh_token: payload.refresh_token,
    expires_at: payload.expires_in ? Math.floor(Date.now() / 1000) + payload.expires_in : undefined,
    user: { id: payload.user.id, email: payload.user.email },
  };
}

export async function signInWithPassword(email: string, password: string): Promise<SupabaseSession> {
  if (!isSupabaseConfigured()) throw new Error('Supabase não configurado. Use o modo local ou preencha as variáveis VITE_SUPABASE_URL e VITE_SUPABASE_ANON_KEY.');
  const response = await fetch(`${supabaseConfig.url}/auth/v1/token?grant_type=password`, {
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

export async function signUpWithPassword(email: string, password: string): Promise<SupabaseSession> {
  if (!isSupabaseConfigured()) throw new Error('Supabase não configurado. Use o modo local ou preencha as variáveis VITE_SUPABASE_URL e VITE_SUPABASE_ANON_KEY.');
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
  if (!isSupabaseConfigured()) throw new Error('Modo local ativo. Supabase não configurado.');
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
