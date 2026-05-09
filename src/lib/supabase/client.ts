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

export interface SupabaseErrorDetails {
  status: number;
  path: string;
  method: string;
  response: unknown;
}

export class SupabaseRequestError extends Error {
  details: SupabaseErrorDetails;

  constructor(message: string, details: SupabaseErrorDetails) {
    super(message);
    this.name = 'SupabaseRequestError';
    this.details = details;
  }
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

export function summarizeSupabaseResponse(response: unknown): string {
  if (!response) return 'Sem detalhes retornados pelo Supabase.';
  if (typeof response === 'string') return response.slice(0, 240);
  if (typeof response === 'object') {
    const payload = response as { message?: unknown; details?: unknown; hint?: unknown; code?: unknown; error?: unknown };
    return [payload.message, payload.details, payload.hint, payload.code, payload.error]
      .filter(Boolean)
      .map(String)
      .join(' | ')
      .slice(0, 360) || JSON.stringify(response).slice(0, 360);
  }
  return String(response).slice(0, 240);
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
  const method = init.method ?? 'GET';
  const response = await fetch(`${supabaseConfig.url}${path}`, {
    ...init,
    headers: { ...authHeaders(session.access_token), Prefer: 'return=representation', ...(init.headers ?? {}) },
  });
  const text = await response.text();
  let data: unknown = [];
  try {
    data = text ? JSON.parse(text) : [];
  } catch {
    data = text;
  }
  if (!response.ok) {
    const details = { status: response.status, path, method, response: data };
    console.error('Erro Supabase:', details);
    throw new SupabaseRequestError(`Supabase ${method} ${path} falhou (${response.status}): ${summarizeSupabaseResponse(data)}`, details);
  }
  return data as T;
}
