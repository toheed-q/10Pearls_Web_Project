import type { FormEvent, ReactNode } from 'react';
import './AuthForm.css';

interface Props {
  title: string;
  error: string | null;
  loading: boolean;
  onSubmit: (e: FormEvent) => void;
  children: ReactNode;
  footer: ReactNode;
}

export function AuthForm({ title, error, loading, onSubmit, children, footer }: Props) {
  return (
    <div className="auth-wrapper">
      <div className="auth-card">
        <h2 className="auth-title">{title}</h2>
        {error && <p className="auth-error">{error}</p>}
        <form onSubmit={onSubmit} noValidate>
          {children}
          <button type="submit" className="auth-btn" disabled={loading}>
            {loading ? 'Please wait…' : title}
          </button>
        </form>
        <div className="auth-footer">{footer}</div>
      </div>
    </div>
  );
}
