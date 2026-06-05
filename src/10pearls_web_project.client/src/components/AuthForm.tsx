import type { FormEvent, ReactNode } from 'react';
import { ThemeToggle } from './ThemeToggle';
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
      <div className="auth-theme-btn"><ThemeToggle /></div>
      <div className="auth-card">
        <div className="auth-brand">
          <div className="auth-brand-icon">
            <span className="auth-brand-icon-inner">
              <svg viewBox="0 0 24 24" fill="none" stroke="#fff" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" width="18" height="18">
                <polyline points="20 6 9 17 4 12"/>
              </svg>
            </span>
          </div>
          <span className="auth-brand-name">TaskFlow</span>
        </div>
        <h2 className="auth-title">{title}</h2>

        {error && (
          <div className="auth-error">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14" style={{ flexShrink: 0 }}>
              <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
            </svg>
            {error}
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
