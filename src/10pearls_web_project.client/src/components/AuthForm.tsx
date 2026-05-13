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
        <div className="auth-brand">
          <span className="auth-brand-icon">✦</span>
          <span className="auth-brand-name">TaskFlow</span>
        </div>
        <h2 className="auth-title">{title}</h2>

        {error && (
          <div className="auth-error">
            <span>⚠</span> {error}
          </div>
        )}

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
