import { useEffect, useState, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { useAuth } from '../context/AuthContext';
import { adminService } from '../services/adminService';
import { taskHub } from '../services/taskHub';
import { HubEvents } from '../types/hubEvents';
import type { UserSummary, UserRoleChangedPayload } from '../types/auth';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import * as signalR from '@microsoft/signalr';
import './AdminDashboard.css';

export function AdminDashboard() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [users, setUsers]     = useState<UserSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [confirm, setConfirm] = useState<{ user: UserSummary; newRole: string } | null>(null);
  const [saving, setSaving]   = useState(false);

  useEffect(() => {
    if (!isAdmin) { navigate('/', { replace: true }); return; }
    loadUsers();
  }, [isAdmin]);

  const onRoleChanged = useCallback((payload: UserRoleChangedPayload) => {
    setUsers(prev => prev.map(u => u.id === payload.userId ? { ...u, role: payload.newRole } : u));
  }, []);

  useEffect(() => {
    taskHub.on(HubEvents.UserRoleChanged, onRoleChanged);
    if (taskHub.state === signalR.HubConnectionState.Disconnected)
      taskHub.start().catch(err => console.error('SignalR start error:', err));
    return () => { taskHub.off(HubEvents.UserRoleChanged, onRoleChanged); };
  }, [onRoleChanged]);

  async function loadUsers() {
    setLoading(true);
    try { setUsers(await adminService.getUsers()); }
    catch { show('Failed to load users', 'error'); }
    finally { setLoading(false); }
  }

  async function confirmRoleChange() {
    if (!confirm) return;
    setSaving(true);
    try {
      await adminService.updateRole(confirm.user.id, confirm.newRole);
      show(`${confirm.user.fullName} is now ${confirm.newRole}`);
    } catch (err: unknown) {
      show(err instanceof Error ? err.message : 'Role update failed', 'error');
    } finally { setSaving(false); setConfirm(null); }
  }

  function handleLogout() { logout(); navigate('/signin'); }

  return (
    <div className="dashboard">

      {/* ── Sticky Navbar ── */}
      <header className="dashboard-header">
        <div className="header-brand">
          <div className="brand-logo">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeLinecap="round" strokeLinejoin="round">
              <polyline points="20 6 9 17 4 12"/>
            </svg>
          </div>
          <div className="brand-text">
            <span className="brand-name">TaskFlow</span>
            <span className="brand-sub">Task Manager</span>
          </div>
        </div>

        <div className="header-title-group">
          <div className="title-row">
            <h1 className="dashboard-title">User Management</h1>
            <span className="admin-badge">Admin</span>
          </div>
          <p className="dashboard-subtitle">Welcome, {user?.fullName}</p>
        </div>

        <div className="header-actions">
          <button className="btn-primary" onClick={() => navigate('/')}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
              <polyline points="15 18 9 12 15 6"/>
            </svg>
            Tasks
          </button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      <div className="dashboard-inner">

        {loading ? (
          <div className="loading-state">
            <div className="spinner" />
            <p>Loading users…</p>
          </div>
        ) : (
          <div className="admin-table-wrap glass">
            <div className="admin-table-scroll">
              <table className="admin-table">
                <thead>
                  <tr>
                    <th>Name</th>
                    <th>Email</th>
                    <th>Role</th>
                    <th>Tasks</th>
                    <th>Actions</th>
                  </tr>
                </thead>
                <tbody>
                  {users.map(u => (
                    <tr key={u.id}>
                      <td>
                        <div className="user-cell">
                          <div className="user-cell-avatar">
                            {u.fullName.charAt(0).toUpperCase()}
                          </div>
                          <span>{u.fullName}</span>
                        </div>
                      </td>
                      <td className="muted">{u.email}</td>
                      <td>
                        <span className={`role-badge role-${u.role.toLowerCase()}`}>{u.role}</span>
                      </td>
                      <td className="muted">{u.taskCount}</td>
                      <td>
                        {u.role === 'User' ? (
                          <button
                            className="btn-action btn-promote"
                            onClick={() => setConfirm({ user: u, newRole: 'Admin' })}
                            disabled={u.id === user?.id}
                          >
                            Promote
                          </button>
                        ) : (
                          <button
                            className="btn-action btn-demote"
                            onClick={() => setConfirm({ user: u, newRole: 'User' })}
                            disabled={u.id === user?.id}
                          >
                            Demote
                          </button>
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </div>
        )}

      </div>

      {confirm && (
        <div className="modal-overlay">
          <div className="modal glass">
            <div className="modal-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="10"/><line x1="12" y1="8" x2="12" y2="12"/><line x1="12" y1="16" x2="12.01" y2="16"/>
              </svg>
            </div>
            <p className="modal-msg">
              {confirm.newRole === 'Admin'
                ? `Promote ${confirm.user.fullName} to Admin?`
                : `Demote ${confirm.user.fullName} to User?`}
            </p>
            <div className="modal-actions">
              <button className="btn-primary" onClick={confirmRoleChange} disabled={saving}>
                {saving ? 'Saving…' : 'Confirm'}
              </button>
              <button className="btn-logout" onClick={() => setConfirm(null)} disabled={saving}>
                Cancel
              </button>
            </div>
          </div>
        </div>
      )}

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  );
}
