import { create } from 'zustand';
import { getStoredSession, isSupabaseConfigured, signInWithPassword, signOut, signUpWithPassword, type SupabaseSession } from '../../lib/supabase/client';
import type { SyncStatus } from '../../lib/sync/syncTypes';

interface AuthState {
  configured: boolean;
  session: SupabaseSession | null;
  status: SyncStatus;
  message?: string;
  loadSession: () => void;
  setStatus: (status: SyncStatus, message?: string) => void;
  signIn: (email: string, password: string) => Promise<void>;
  signUp: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  configured: isSupabaseConfigured(),
  session: getStoredSession(),
  status: isSupabaseConfigured() && getStoredSession() ? 'online' : 'local',
  loadSession() {
    const session = getStoredSession();
    set({ configured: isSupabaseConfigured(), session, status: isSupabaseConfigured() && session ? 'online' : 'local' });
  },
  setStatus(status, message) {
    set({ status, message });
  },
  async signIn(email, password) {
    const session = await signInWithPassword(email, password);
    set({ session, status: 'online', message: 'Login realizado. Sincronização disponível.' });
  },
  async signUp(email, password) {
    const session = await signUpWithPassword(email, password);
    if (session) set({ session, status: 'online', message: 'Conta criada. Sincronização disponível.' });
    else set({ session: null, status: 'local', message: 'Conta criada. Confirme seu email e depois entre para sincronizar.' });
  },
  async logout() {
    await signOut(get().session);
    set({ session: null, status: isSupabaseConfigured() ? 'local' : 'local', message: 'Você saiu da conta online. O modo local continua disponível.' });
  },
}));
