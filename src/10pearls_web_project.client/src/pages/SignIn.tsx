import { type FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthForm } from '../components/AuthForm';
import { InputField } from '../components/InputField';
import { authService } from '../services/authService';
import { useAuth } from '../context/AuthContext';

function validateEmail(v: string): string | null {
  if (!v) return 'Email is required';
  if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(v)) return 'Enter a valid email address (e.g. you@example.com)';
  return null;
}

function validatePassword(v: string): string | null {
  if (!v) return 'Password is required';
  if (v.length < 6) return 'Password must be at least 6 characters';
  return null;
}

export function SignIn() {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const { setAuth } = useAuth();
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    // Run all validations before submitting
    if (validateEmail(email) || validatePassword(password)) {
      setError('Please fix the errors above before continuing.');
      return;
    }
    setError(null);
    setLoading(true);
    try {
      const data = await authService.login({ email, password });
      setAuth({ id: data.id, email: data.email, fullName: data.fullName, role: data.role }, data.token);
      navigate('/');
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : 'Login failed');
    } finally {
      setLoading(false);
    }
  }

  return (
    <AuthForm
      title="Sign In"
      error={error}
      loading={loading}
      onSubmit={handleSubmit}
      footer={<>Don't have an account? <Link to="/signup">Sign Up</Link></>}
    >
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
        placeholder="Password"
        icon="🔒"
        validate={validatePassword}
        showToggle
      />
    </AuthForm>
  );
}
