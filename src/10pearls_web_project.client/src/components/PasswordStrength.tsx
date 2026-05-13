interface Props {
  password: string;
}

export type StrengthLevel = 'weak' | 'medium' | 'strong' | '';

export function getStrength(pw: string): StrengthLevel {
  if (!pw) return '';
  const hasMin    = pw.length >= 6;
  const hasNumber = /\d/.test(pw);
  const hasSpecial = /[^a-zA-Z0-9]/.test(pw);
  const hasUpper  = /[A-Z]/.test(pw);

  const score = [hasMin, hasNumber, hasSpecial, hasUpper].filter(Boolean).length;

  if (score <= 1) return 'weak';
  if (score === 2 || score === 3) return 'medium';
  return 'strong';
}

const LABELS: Record<StrengthLevel, string> = {
  '':       '',
  weak:     'Weak',
  medium:   'Medium — accepted',
  strong:   'Strong',
};

const HINTS: Record<StrengthLevel, string> = {
  '':       'Min. 6 chars, 1 number, 1 special character',
  weak:     'Add a number or special character (!@#$…)',
  medium:   'Good! Add uppercase + special char for strong',
  strong:   'Great password!',
};

export function PasswordStrength({ password }: Props) {
  const level = getStrength(password);
  if (!password) return (
    <p className="pw-hint">Min. 6 chars · 1 number · 1 special character</p>
  );

  const filled = level === 'weak' ? 1 : level === 'medium' ? 2 : 3;

  return (
    <div className="pw-strength">
      <div className="pw-strength-bars">
        {[0, 1, 2].map(i => (
          <div key={i} className={`pw-bar ${i < filled ? level : ''}`} />
        ))}
      </div>
      <span className={`pw-strength-label ${level}`}>{LABELS[level]}</span>
      <p className="pw-hint">{HINTS[level]}</p>
    </div>
  );
}
