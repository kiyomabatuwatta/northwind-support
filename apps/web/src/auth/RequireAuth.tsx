import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';

/** Where to go after login: the page the visitor originally wanted. */
export interface LoginRedirectState {
  from?: string;
}

/** Shows its children to signed-in agents; everyone else is sent to /login and brought back. */
export function RequireAuth({ children }: { children: ReactNode }) {
  const { isSignedIn } = useAuth();
  const location = useLocation();

  if (!isSignedIn) {
    const state: LoginRedirectState = { from: `${location.pathname}${location.search}` };

    return <Navigate to="/login" replace state={state} />;
  }

  return <>{children}</>;
}
