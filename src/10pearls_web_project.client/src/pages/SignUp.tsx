import { type FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthForm } from '../components/AuthForm';
import { InputField } from '../components/InputField';
import { PasswordStrength, getStrength } from '../components/PasswordStrength';
import { authService } from '../services/authService';

function validateFullName(v: string): string | null {
  if (!v.trim()) return 'Full name is required';
  if (v.trim().length < 2) return 'Name must be at least 2 characters';
  return null;
}

function validateEmail(v: string): string | null {
  if (!v) return 'Email is required';
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return 'Enter a valid email address (e.g. you@example.com)';
  return null;
}

function validatePassword(v: string): string | null {
  if (!v) return 'Password is required';
  if (v.length < 6) return 'Password must be at least 6 characters';
  const strength = getStrength(v);
  if (strength === 'weak') return 'Password is too weak — add a number or special character';
  return null;
}

export function SignUp() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    if (validateFullName(fullName) || validateEmail(email) || validatePassword(password)) {
      setError('Please fix the errors above before continuing.');
      return;
    }
    setError(null);
    setLoading(true);
    try {
      await authService.register({ fullName, email, password });
      navigate('/signin');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Registration failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthForm
      title="Create Account"
      error={error}
      loading={loading}
      onSubmit={handleSubmit}
      footer={<>Already have an account? <Link to="/signin">Sign In</Link></>}
    >
      <InputField
        label="Full Name"
        type="text"
        value={fullName}
        onChange={setFullName}
        required
        placeholder="Full name"
        icon="👤"
        validate={validateFullName}
      />
      <InputField
        label="Email"
        type="email"
        value={email}
        onChange={setEmail}
        required
        placeholder="Email address"
        icon="✉"
        validate={validateEmail}
      />
      <InputField
        label="Password"
        type="password"
        value={password}
        onChange={setPassword}
        required
        placeholder="Create a password"
        icon="🔒"
        validate={validatePassword}
        showToggle
      />
      {/* Live password strength — shown as soon as user starts typing */}
      {password.length > 0 && <PasswordStrength password={password} />}
    </AuthForm>
  );
}
