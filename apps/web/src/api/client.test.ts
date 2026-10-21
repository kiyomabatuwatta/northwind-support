import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { clearSession, saveSession } from '../auth/session';
import { request, setUnauthorizedHandler } from './client';

const fetchMock = vi.fn<typeof fetch>();

function jsonResponse(status: number, body: unknown): Response {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

describe('request', () => {
  beforeEach(() => {
    vi.stubGlobal('fetch', fetchMock);
    clearSession();
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    fetchMock.mockReset();
    setUnauthorizedHandler(undefined);
  });

  it('attaches the stored token to the call', async () => {
    saveSession({
      token: 'jwt-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'a@b.test' },
    });
    fetchMock.mockResolvedValue(jsonResponse(200, { ok: true }));

    await request('/tickets');

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer jwt-token');
  });

  it('sends no Authorization header when nobody is signed in', async () => {
    fetchMock.mockResolvedValue(jsonResponse(200, {}));

    await request('/tickets');

    const headers = fetchMock.mock.calls[0]?.[1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBeUndefined();
  });

  it('clears the session and notifies the app on a 401', async () => {
    saveSession({
      token: 'expired',
      expiresAtUtc: '2999-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'a@b.test' },
    });
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    fetchMock.mockResolvedValue(jsonResponse(401, { title: 'Unauthorized' }));

    await expect(request('/tickets')).rejects.toMatchObject({ status: 401 });

    expect(onUnauthorized).toHaveBeenCalledTimes(1);
    expect(window.sessionStorage.length).toBe(0);
  });

  it('leaves the session alone when a 401 is expected (login)', async () => {
    const onUnauthorized = vi.fn();
    setUnauthorizedHandler(onUnauthorized);
    fetchMock.mockResolvedValue(jsonResponse(401, { title: 'Unauthorized' }));

    await expect(request('/auth/login', { handleUnauthorized: false })).rejects.toMatchObject({ status: 401 });

    expect(onUnauthorized).not.toHaveBeenCalled();
  });
});
