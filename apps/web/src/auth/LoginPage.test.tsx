import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { authApi } from '../api/auth';
import { ApiError } from '../api/client';
import { AuthProvider } from './AuthContext';
import { LoginPage } from './LoginPage';
import { RequireAuth } from './RequireAuth';
import { loadSession } from './session';

vi.mock('../api/auth', () => ({ authApi: { login: vi.fn(), me: vi.fn() } }));

const login = vi.mocked(authApi.login);

function renderApp(initialPath: string) {
  return render(
    <AuthProvider>
      <MemoryRouter initialEntries={[initialPath]}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/tickets/7"
            element={
              <RequireAuth>
                <p>Ticket seven</p>
              </RequireAuth>
            }
          />
        </Routes>
      </MemoryRouter>
    </AuthProvider>,
  );
}

describe('login', () => {
  beforeEach(() => window.sessionStorage.clear());
  afterEach(() => vi.clearAllMocks());

  it('sends an unauthenticated visitor to the login page instead of the protected page', () => {
    renderApp('/tickets/7');

    expect(screen.getByRole('heading', { name: 'Sign in' })).toBeInTheDocument();
    expect(screen.queryByText('Ticket seven')).not.toBeInTheDocument();
  });

  it('blocks an empty form and never calls the API', async () => {
    const user = userEvent.setup();
    renderApp('/login');

    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(screen.getByText('Enter a valid email address.')).toBeInTheDocument();
    expect(screen.getByText('Enter your password.')).toBeInTheDocument();
    expect(login).not.toHaveBeenCalled();
  });

  it('shows a clear error for rejected credentials and stores nothing', async () => {
    const user = userEvent.setup();
    login.mockRejectedValue(new ApiError(401, 'Invalid email or password.'));
    renderApp('/login');

    await user.type(screen.getByLabelText('Email'), 'alex.turner@northwind-support.example');
    await user.type(screen.getByLabelText('Password'), 'wrong');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Incorrect email or password.');
    expect(loadSession()).toBeNull();
  });

  it('signs in, keeps the session and returns to the page the visitor wanted', async () => {
    const user = userEvent.setup();
    login.mockResolvedValue({
      token: 'jwt-token',
      expiresAtUtc: '2999-01-01T00:00:00Z',
      user: { id: 1, fullName: 'Alex Turner', email: 'alex.turner@northwind-support.example' },
    });
    renderApp('/tickets/7');

    await user.type(screen.getByLabelText('Email'), 'alex.turner@northwind-support.example');
    await user.type(screen.getByLabelText('Password'), 'Dev-Password-1!');
    await user.click(screen.getByRole('button', { name: 'Sign in' }));

    expect(await screen.findByText('Ticket seven')).toBeInTheDocument();
    expect(loadSession()?.token).toBe('jwt-token');
  });
});
