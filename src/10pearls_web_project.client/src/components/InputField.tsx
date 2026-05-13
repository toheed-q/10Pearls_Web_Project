import { useState } from 'react';

interface Props {
  label: string;
  type: string;
  value: string;
  onChange: (v: string) => void;
  required?: boolean;
  placeholder?: string;
  icon?: string;
  validate?: (v: string) => string | null; // returns error string or null
  showToggle?: boolean; // for password fields
}

export function InputField({
  label,
  type,
  value,
  onChange,
  required,
  placeholder,
  icon,
  validate,
  showToggle,
}: Props) {
  const [touched, setTouched] = useState(false);
  const [showPw, setShowPw] = useState(false);

  const error = touched && validate ? validate(value) : null;
  const isValid = touched && !error && value.length > 0;
  const inputType = showToggle ? (showPw ? 'text' : 'password') : type;

  return (
    <div className="auth-field">
      <label>{label}</label>
      <div className={`auth-input-wrap ${error ? 'has-error' : ''} ${isValid ? 'is-valid' : ''}`}>
        {icon && <span className="auth-input-icon">{icon}</span>}
        <input
          type={inputType}
          value={value}
          placeholder={placeholder}
          required={required}
          onChange={e => onChange(e.target.value)}
          onBlur={() => setTouched(true)}
        />
        {showToggle && (
          <button
            type="button"
            className="auth-toggle-pw"
            onClick={() => setShowPw(p => !p)}
            tabIndex={-1}
            aria-label={showPw ? 'Hide password' : 'Show password'}
          >
            {showPw ? '🙈' : '👁'}
          </button>
        )}
      </div>
      {error && <p className="field-error"><span>✕</span> {error}</p>}
    </div>
  );
}
