import type { CurrentUser, LoginResponse } from '../types/api';
import { request } from './client';

export const authApi = {
  /** A 401 here means wrong credentials, so it must not trigger the "session expired" redirect. */
  login: (email: string, password: string) =>
    request<LoginResponse>('/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
      handleUnauthorized: false,
    }),

  me: () => request<CurrentUser>('/auth/me'),
};
