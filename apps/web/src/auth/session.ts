import type { CurrentUser } from '../types/api';

/** What is kept after a successful login. */
export interface Session {
  token: string;
  expiresAtUtc: string;
  user: CurrentUser;
}

const STORAGE_KEY = 'supportdesk.session';

/**
 * The session lives in sessionStorage: it survives a page refresh but is dropped when the tab
 * closes. The trade-off is that script running in the page can read it (unlike an httpOnly
 * cookie), so the app avoids injecting raw HTML and tokens are short-lived (one hour).
 */
export function loadSession(now: Date = new Date()): Session | null {
  try {
    const raw = window.sessionStorage.getItem(STORAGE_KEY);

    if (!raw) {
      return null;
    }

    const session = JSON.parse(raw) as Session;

    if (!session.token || new Date(session.expiresAtUtc).getTime() <= now.getTime()) {
      clearSession();
      return null;
    }

    return session;
  } catch {
    return null;
  }
}

export function saveSession(session: Session): void {
  try {
    window.sessionStorage.setItem(STORAGE_KEY, JSON.stringify(session));
  } catch {
    // Storage can be blocked; the user is simply asked to sign in again after a refresh.
  }
}

export function clearSession(): void {
  try {
    window.sessionStorage.removeItem(STORAGE_KEY);
  } catch {
    // Nothing to clear if storage is unavailable.
  }
}

/** The bearer token for the API, or undefined when nobody is signed in. */
export function currentToken(): string | undefined {
  return loadSession()?.token;
}
