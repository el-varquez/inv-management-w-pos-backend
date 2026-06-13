import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth';

export const LoginScreen = () => {
  const { login, loading, error, token } = useAuth();
  const navigate = useNavigate();
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');

  // Once authenticated, leave the login screen.
  useEffect(() => {
    if (token) navigate('/items', { replace: true });
  }, [token, navigate]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    await login(email, password);
  };

  const fillDemo = () => {
    setEmail('admin@pos.local');
    setPassword('Admin123!');
  };

  return (
    <div className="login-wrap">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-brand">
          <div className="brand-mark">T</div>
          <div>
            <div className="brand-name">Tindahan</div>
            <div className="brand-sub">POS &amp; Inventory</div>
          </div>
        </div>

        <p className="eyebrow">Admin access</p>
        <h1 className="login-title">Welcome back</h1>
        <p className="login-lead">
          Sign in to manage your items, stock, and sales.
        </p>

        {error && (
          <div className="login-error" role="alert">
            <span aria-hidden="true">⚠</span>
            {error}
          </div>
        )}

        <div className="field">
          <label htmlFor="email">Email</label>
          <input
            id="email"
            className="input"
            type="email"
            autoComplete="username"
            placeholder="you@store.ph"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />
        </div>

        <div className="field">
          <label htmlFor="password">Password</label>
          <input
            id="password"
            className="input"
            type="password"
            autoComplete="current-password"
            placeholder="••••••••"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            required
          />
        </div>

        <button
          type="submit"
          className="btn btn-primary btn-block"
          disabled={loading}
        >
          {loading ? <span className="spinner" aria-hidden="true" /> : null}
          {loading ? 'Signing in…' : 'Sign in'}
        </button>

        <div className="login-demo">
          <span>
            Demo: <code>admin@pos.local</code> / <code>Admin123!</code>
          </span>
          <button type="button" className="btn btn-quiet" onClick={fillDemo}>
            Use demo
          </button>
        </div>
      </form>
    </div>
  );
};
