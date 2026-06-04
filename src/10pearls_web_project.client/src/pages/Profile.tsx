import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { profileService } from '../services/profileService';
import { taskHub } from '../services/taskHub';
import { HubEvents } from '../types/hubEvents';
import type { UserProfileDto } from '../types/auth';
import type { Task } from '../types/task';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import * as signalR from '@microsoft/signalr';
import './Profile.css';

export function Profile() {
  const { user, setAuth, token, logout } = useAuth();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [profile, setProfile]   = useState<UserProfileDto | null>(null);
  const [loading, setLoading]   = useState(true);
  const [saving, setSaving]     = useState(false);

  // Edit form state
  const [fullName,         setFullName]         = useState('');
  const [currentPassword, setCurrentPassword]  = useState('');
  const [newPassword,      setNewPassword]      = useState('');
  const [confirmPassword,  setConfirmPassword]  = useState('');

  useEffect(() => { load(); }, []);

  async function load() {
    setLoading(true);
    try {
      const p = await profileService.getProfile();
      setProfile(p);
      setFullName(p.fullName);
    } catch {
      show('Failed to load profile', 'error');
    } finally {
      setLoading(false);
    }
  }

  // ── Real-time stats refresh ───────────────────────────────────────────
  const refreshStats = useCallback(() => {
    profileService.getProfile().then(p => setProfile(p)).catch(() => {});
  }, []);

  useEffect(() => {
    const onCreated = (_: Task)   => refreshStats();
    const onDeleted = (_: string) => refreshStats();
    const onChanged = (_: Task)   => refreshStats();

    taskHub.on(HubEvents.TaskCreated,       onCreated);
    taskHub.on(HubEvents.TaskDeleted,       onDeleted);
    taskHub.on(HubEvents.TaskStatusChanged, onChanged);

    if (taskHub.state === signalR.HubConnectionState.Disconnected)
      taskHub.start().catch(() => {});

    return () => {
      taskHub.off(HubEvents.TaskCreated,       onCreated);
      taskHub.off(HubEvents.TaskDeleted,       onDeleted);
      taskHub.off(HubEvents.TaskStatusChanged, onChanged);
    };
  }, [refreshStats]);

  async function handleSave(e: React.FormEvent) {
    e.preventDefault();

    const changingPassword = currentPassword || newPassword || confirmPassword;

    if (changingPassword) {
      if (!currentPassword) { show('Enter your current password', 'error'); return; }
      if (!newPassword)      { show('Enter a new password', 'error');       return; }
      if (newPassword !== confirmPassword) { show('Passwords do not match', 'error'); return; }
    }

    if (!fullName.trim()) { show('Name cannot be empty', 'error'); return; }

    setSaving(true);
    try {
      await profileService.updateProfile({
        fullName:        fullName.trim() !== profile?.fullName ? fullName.trim() : undefined,
        currentPassword: changingPassword ? currentPassword : undefined,
        newPassword:     changingPassword ? newPassword     : undefined,
      });

      show('Profile updated');
      setCurrentPassword('');
      setNewPassword('');
      setConfirmPassword('');

      // Refresh profile data + update auth context so header name updates
      const updated = await profileService.getProfile();
      setProfile(updated);
      if (token) setAuth({ ...user!, fullName: updated.fullName }, token);
    } catch (err: unknown) {
      show(err instanceof Error ? err.message : 'Update failed', 'error');
    } finally {
      setSaving(false);
    }
  }

  function handleLogout() { logout(); navigate('/signin'); }

  if (loading) return <div className="dashboard"><div className="loading-state">Loading profile…</div></div>;
  if (!profile) return <div className="dashboard"><div className="loading-state">Profile not found.</div></div>;

  const completionPct = profile.taskStats.total > 0
    ? Math.round((profile.taskStats.completed / profile.taskStats.total) * 100)
    : 0;

  return (
    <div className="dashboard">

      <header className="dashboard-header">
        <div className="header-title-group">
          <div className="title-row">
            <h1 className="dashboard-title">My Profile</h1>
            <span className={`role-badge role-${profile.role.toLowerCase()}`}>{profile.role}</span>
          </div>
          <p className="dashboard-subtitle">Manage your account</p>
        </div>
        <div className="header-actions">
          <button className="btn-primary" onClick={() => navigate('/')}>← Dashboard</button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      <div className="profile-layout">

        {/* ── Identity card ── */}
        <div className="profile-card glass">
          <div className="profile-avatar">
            {profile.fullName.charAt(0).toUpperCase()}
          </div>
          <h2 className="profile-name">{profile.fullName}</h2>
          <p className="profile-email">{profile.email}</p>
          <span className={`role-badge role-${profile.role.toLowerCase()}`}>{profile.role}</span>

          {/* ── Stats summary inside card ── */}
          <div className="profile-mini-stats">
            <div className="mini-stat">
              <span className="mini-stat-val">{profile.taskStats.total}</span>
              <span className="mini-stat-lbl">Total</span>
            </div>
            <div className="mini-stat">
              <span className="mini-stat-val" style={{ color: 'var(--status-completed)' }}>{profile.taskStats.completed}</span>
              <span className="mini-stat-lbl">Done</span>
            </div>
            <div className="mini-stat">
              <span className="mini-stat-val" style={{ color: 'var(--status-pending)' }}>{profile.taskStats.pending}</span>
              <span className="mini-stat-lbl">Pending</span>
            </div>
          </div>

          {/* ── Completion bar ── */}
          <div className="progress-section">
            <div className="progress-header">
              <span>Completion</span>
              <span className="progress-pct">{completionPct}%</span>
            </div>
            <div className="progress-track">
              <div className="progress-fill" style={{ width: `${completionPct}%` }} />
            </div>
          </div>
        </div>

        {/* ── Edit form ── */}
        <div className="profile-card glass edit-card">
          <h3 className="edit-section-title">Edit Profile</h3>

          <form onSubmit={handleSave} className="edit-form">

            <div className="form-group">
              <label className="form-label">Full Name</label>
              <input
                className="form-input"
                type="text"
                value={fullName}
                onChange={e => setFullName(e.target.value)}
                placeholder="Your full name"
              />
            </div>

            <div className="form-divider">
              <span>Change Password <span className="form-optional">(optional)</span></span>
            </div>

            <div className="form-group">
              <label className="form-label">Current Password</label>
              <input
                className="form-input"
                type="password"
                value={currentPassword}
                onChange={e => setCurrentPassword(e.target.value)}
                placeholder="Enter current password"
                autoComplete="current-password"
              />
            </div>

            <div className="form-group">
              <label className="form-label">New Password</label>
              <input
                className="form-input"
                type="password"
                value={newPassword}
                onChange={e => setNewPassword(e.target.value)}
                placeholder="Enter new password"
                autoComplete="new-password"
              />
            </div>

            <div className="form-group">
              <label className="form-label">Confirm New Password</label>
              <input
                className={`form-input ${confirmPassword && newPassword !== confirmPassword ? 'input-error' : ''}`}
                type="password"
                value={confirmPassword}
                onChange={e => setConfirmPassword(e.target.value)}
                placeholder="Confirm new password"
                autoComplete="new-password"
              />
              {confirmPassword && newPassword !== confirmPassword && (
                <span className="field-error">Passwords do not match</span>
              )}
            </div>

            <button className="btn-primary btn-save" type="submit" disabled={saving}>
              {saving ? 'Saving…' : 'Save Changes'}
            </button>
          </form>
        </div>

      </div>

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  );
}
