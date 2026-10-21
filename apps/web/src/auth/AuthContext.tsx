import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { authApi } from '../api/auth';
import { setUnauthorizedHandler } from '../api/client';
import type { CurrentUser } from '../types/api';
import { clearSession, loadSession, saveSession, type Session } from './session';

interface AuthContextValue {
  user: CurrentUser | null;
  isSignedIn: boolean;
  /** Rejects with an ApiError (401) when the credentials are wrong. */
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<Session | null>(() => loadSession());

  const logout = useCallback(() => {
    clearSession();
    setSession(null);
  }, []);

  const login = useCallback(async (email: string, password: string) => {
    const response = await authApi.login(email, password);
    const next: Session = {
      token: response.token,
      expiresAtUtc: response.expiresAtUtc,
      user: response.user,
    };

    saveSession(next);
    setSession(next);
  }, []);

  // Any 401 from the API (an expired token, say) ends the session; protected routes then
  // redirect to the login page because there is no user any more.
  useEffect(() => {
    setUnauthorizedHandler(() => setSession(null));

    return () => setUnauthorizedHandler(undefined);
  }, []);

  const value = useMemo<AuthContextValue>(
    () => ({ user: session?.user ?? null, isSignedIn: session !== null, login, logout }),
    [session, login, logout],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const value = useContext(AuthContext);

  if (!value) {
    throw new Error('useAuth must be used inside an AuthProvider.');
  }

  return value;
}
