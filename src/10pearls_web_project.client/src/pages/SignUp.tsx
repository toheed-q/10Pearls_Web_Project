import { type FormEvent, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { AuthForm } from '../components/AuthForm';
import { InputField } from '../components/InputField';
import { authService } from '../services/authService';

export function SignUp() {
  const [fullName, setFullName] = useState('');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const navigate = useNavigate();

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
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
      title="Sign Up"
      error={error}
      loading={loading}
      onSubmit={handleSubmit}
      footer={<>Already have an account? <Link to="/signin">Sign In</Link></>}
    >
      <InputField label="Full Name" type="text" value={fullName} onChange={setFullName} required placeholder="John Doe" />
      <InputField label="Email" type="email" value={email} onChange={setEmail} required placeholder="you@example.com" />
      <InputField label="Password" type="password" value={password} onChange={setPassword} required placeholder="Min. 6 characters" />
    </AuthForm>
  );
}
