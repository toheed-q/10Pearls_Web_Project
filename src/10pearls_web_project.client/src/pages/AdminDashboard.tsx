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

  const [users, setUsers]           = useState<UserSummary[]>([]);
  const [loading, setLoading]       = useState(true);
  const [confirm, setConfirm]       = useState<{ user: UserSummary; newRole: string } | null>(null);
  const [saving, setSaving]         = useState(false);

  useEffect(() => {
    if (!isAdmin) { navigate('/', { replace: true }); return; }
    loadUsers();
  }, [isAdmin]);

  // ── SignalR: subscribe to role changes ──────────────────────────────────
  const onRoleChanged = useCallback((payload: UserRoleChangedPayload) => {
    setUsers(prev =>
      prev.map(u => u.id === payload.userId ? { ...u, role: payload.newRole } : u)
    );
  }, []);

  useEffect(() => {
    taskHub.on(HubEvents.UserRoleChanged, onRoleChanged);

    if (taskHub.state === signalR.HubConnectionState.Disconnected) {
      taskHub.start().catch(err => console.error('SignalR start error:', err));
    }

    return () => { taskHub.off(HubEvents.UserRoleChanged, onRoleChanged); };
  }, [onRoleChanged]);

  async function loadUsers() {
    setLoading(true);
    try {
      setUsers(await adminService.getUsers());
    } catch {
      show('Failed to load users', 'error');
    } finally {
      setLoading(false);
    }
  }

  async function confirmRoleChange() {
    if (!confirm) return;
    setSaving(true);
    try {
      await adminService.updateRole(confirm.user.id, confirm.newRole);
      show(`${confirm.user.fullName} is now ${confirm.newRole}`);
      // SignalR will update the list — no manual state mutation needed
    } catch (err: unknown) {
      show(err instanceof Error ? err.message : 'Role update failed', 'error');
    } finally {
      setSaving(false);
      setConfirm(null);
    }
  }

  function handleLogout() { logout(); navigate('/signin'); }

  return (
    <div className="dashboard">
      <header className="dashboard-header">
        <div className="header-title-group">
          <div className="title-row">
            <h1 className="dashboard-title">User Management</h1>
            <span className="admin-badge">Admin</span>
          </div>
          <p className="dashboard-subtitle">Welcome, {user?.fullName}</p>
        </div>
        <div className="header-actions">
          <button className="btn-primary" onClick={() => navigate('/')}>← Tasks</button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      {loading ? (
        <div className="loading-state">Loading users…</div>
      ) : (
        <div className="admin-table-wrap glass">
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
                  <td>{u.fullName}</td>
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
      )}

      {/* ── Confirmation dialog ── */}
      {confirm && (
        <div className="modal-overlay">
          <div className="modal glass">
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
