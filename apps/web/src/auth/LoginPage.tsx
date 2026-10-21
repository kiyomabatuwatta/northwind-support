import { useState, type FormEvent } from 'react';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { ApiError } from '../api/client';
import { Field } from '../components/Field';
import { useAuth } from './AuthContext';
import type { LoginRedirectState } from './RequireAuth';

interface LoginErrors {
  email?: string;
  password?: string;
}

function validateLogin(email: string, password: string): LoginErrors {
  const errors: LoginErrors = {};

  if (!/^\S+@\S+\.\S+$/.test(email.trim())) {
    errors.email = 'Enter a valid email address.';
  }

  if (password.length === 0) {
    errors.password = 'Enter your password.';
  }

  return errors;
}

export function LoginPage() {
  const { isSignedIn, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const target = (location.state as LoginRedirectState | null)?.from ?? '/tickets';

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<LoginErrors>({});
  const [serverError, setServerError] = useState<string>();
  const [isPending, setIsPending] = useState(false);

  if (isSignedIn) {
    return <Navigate to={target} replace />;
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setServerError(undefined);

    const found = validateLogin(email, password);
    setErrors(found);

    if (Object.keys(found).length > 0) {
      return;
    }

    setIsPending(true);

    try {
      await login(email.trim(), password);
      void navigate(target, { replace: true });
    } catch (caught) {
      setServerError(
        caught instanceof ApiError && caught.status === 401
          ? 'Incorrect email or password.'
          : 'Could not sign in. Please try again.',
      );
      setIsPending(false);
    }
  };

  return (
    <main className="login">
      <form className="card login__card" onSubmit={(event) => void submit(event)} noValidate>
        <h1>Sign in</h1>

        <Field id="login-email" label="Email" error={errors.email}>
          {(props) => (
            <input
              {...props}
              type="email"
              autoComplete="username"
              value={email}
              onChange={(event) => setEmail(event.target.value)}
            />
          )}
        </Field>

        <Field id="login-password" label="Password" error={errors.password}>
          {(props) => (
            <input
              {...props}
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(event) => setPassword(event.target.value)}
            />
          )}
        </Field>

        <button type="submit" className="button button--primary" disabled={isPending}>
          {isPending ? 'Signing in...' : 'Sign in'}
        </button>

        {serverError && (
          <p className="field__error" role="alert">
            {serverError}
          </p>
        )}
      </form>
    </main>
  );
}
