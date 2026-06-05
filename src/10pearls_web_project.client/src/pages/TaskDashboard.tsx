import { useEffect, useState, useMemo, useCallback } from 'react';
import * as signalR from '@microsoft/signalr';
import type { AppTaskStatus, CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { taskService, type TaskStats } from '../services/taskService';
import { useAuth } from '../context/AuthContext';
import { useTaskHub } from '../hooks/useTaskHub';
import { TaskList } from '../components/TaskList';
import { TaskForm } from '../components/TaskForm';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import { useNavigate } from 'react-router-dom';
import { ThemeToggle } from '../components/ThemeToggle';
import './TaskDashboard.css';

const PAGE_SIZE = 6;
type SortOrder = 'asc' | 'desc';

function deriveStats(tasks: Task[]): TaskStats {
  return {
    total:      tasks.length,
    pending:    tasks.filter(t => t.status === 'Pending').length,
    inProgress: tasks.filter(t => t.status === 'InProgress').length,
    completed:  tasks.filter(t => t.status === 'Completed').length,
  };
}

export function TaskDashboard() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [tasks, setTasks]         = useState<Task[]>([]);
  const [loading, setLoading]     = useState(true);
  const [saving, setSaving]       = useState(false);
  const [exporting, setExporting] = useState(false);

  const [statusFilter, setStatusFilter] = useState<AppTaskStatus | 'All'>('All');
  const [sortOrder, setSortOrder]       = useState<SortOrder>('asc');
  const [search, setSearch]             = useState('');
  const [page, setPage]                 = useState(1);

  const [showForm, setShowForm]       = useState(false);
  const [editingTask, setEditingTask] = useState<Task | undefined>();

  const onTaskCreated = useCallback((task: Task) => {
    const normalized = { ...task, id: task.id.toLowerCase() };
    setTasks(prev => {
      if (prev.some(t => t.id === normalized.id)) return prev;
      return [normalized, ...prev];
    });
  }, []);

  const onTaskUpdated = useCallback((task: Task) => {
    const normalized = { ...task, id: task.id.toLowerCase() };
    setTasks(prev => prev.map(t => t.id === normalized.id ? normalized : t));
  }, []);

  const onTaskDeleted = useCallback((taskId: string) => {
    setTasks(prev => prev.filter(t => t.id !== taskId.toLowerCase()));
  }, []);

  const onTaskStatusChanged = useCallback((task: Task) => {
    const normalized = { ...task, id: task.id.toLowerCase() };
    setTasks(prev => prev.map(t => t.id === normalized.id ? normalized : t));
  }, []);

  const { connectionState } = useTaskHub({ onTaskCreated, onTaskUpdated, onTaskDeleted, onTaskStatusChanged });

  const stats = useMemo(() => deriveStats(tasks), [tasks]);

  useEffect(() => { loadAll(); }, []);

  async function loadAll() {
    setLoading(true);
    try {
      const data = await taskService.getAll();
      setTasks(data.map(t => ({ ...t, id: t.id.toLowerCase() })));
    } catch {
      show('Failed to load tasks', 'error');
    } finally {
      setLoading(false);
    }
  }

  const filtered = useMemo(() => {
    const q = search.trim().toLowerCase();
    let result = statusFilter === 'All' ? tasks : tasks.filter(t => t.status === statusFilter);
    if (q) result = result.filter(t => t.title.toLowerCase().includes(q));
    result = [...result].sort((a, b) => {
      const diff = new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
      return sortOrder === 'asc' ? diff : -diff;
    });
    return result;
  }, [tasks, statusFilter, sortOrder, search]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  function handleFilterChange(value: AppTaskStatus | 'All') { setStatusFilter(value); setPage(1); }
  function handleSearch(value: string) { setSearch(value); setPage(1); }

  async function handleCreate(dto: CreateTaskDTO) {
    setSaving(true);
    try {
      await taskService.create(dto);
      setShowForm(false);
      show('Task created');
    } finally { setSaving(false); }
  }

  async function handleUpdate(dto: CreateTaskDTO) {
    if (!editingTask) return;
    setSaving(true);
    try {
      const updateDto: UpdateTaskDTO = {
        title: dto.title, description: dto.description,
        dueDate: dto.dueDate, status: dto.status, priority: dto.priority,
      };
      const updated = await taskService.update(editingTask.id, updateDto);
      setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
      setEditingTask(undefined);
      setShowForm(false);
      show('Task updated');
    } finally { setSaving(false); }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this task?')) return;
    try {
      await taskService.delete(id);
      setTasks(prev => prev.filter(t => t.id !== id));
      show('Task deleted');
    } catch { show('Failed to delete task', 'error'); }
  }

  async function handleExportCsv() {
    setExporting(true);
    try {
      await taskService.exportCsv();
      show('CSV exported successfully');
    } catch { show('Failed to export tasks', 'error'); }
    finally { setExporting(false); }
  }

  function handleLogout() { logout(); navigate('/signin'); }

  const connLabel =
    connectionState === signalR.HubConnectionState.Connected    ? 'Live'         :
    connectionState === signalR.HubConnectionState.Reconnecting ? 'Reconnecting' : 'Offline';

  const connClass =
    connectionState === signalR.HubConnectionState.Connected    ? 'conn-live'         :
    connectionState === signalR.HubConnectionState.Reconnecting ? 'conn-reconnecting' : 'conn-offline';

  return (
    <div className="dashboard">

      {/* ── Sticky Navbar ── */}
      <header className="dashboard-header">

        {/* Brand */}
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

        {/* Page title */}
        <div className="header-title-group">
          <div className="title-row">
            <h1 className="dashboard-title">
              {isAdmin ? 'Admin Dashboard' : 'My Tasks'}
            </h1>
            {isAdmin && <span className="admin-badge">Admin</span>}
            <span className={`conn-badge ${connClass}`}>
              <span className="conn-dot" />
              {connLabel}
            </span>
          </div>
          <p className="dashboard-subtitle">
            {isAdmin ? `Global view · ${user?.fullName}` : `Welcome back, ${user?.fullName}`}
          </p>
        </div>

        {/* Actions */}
        <div className="header-actions">
          {isAdmin && (
            <button className="btn-primary" onClick={() => navigate('/admin')}>
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
                <path d="M17 21v-2a4 4 0 0 0-4-4H5a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/>
                <path d="M23 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/>
              </svg>
              Users
            </button>
          )}
          <button className="btn-primary" onClick={() => { setEditingTask(undefined); setShowForm(true); }}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.5" strokeLinecap="round" strokeLinejoin="round" width="13" height="13">
              <line x1="12" y1="5" x2="12" y2="19"/><line x1="5" y1="12" x2="19" y2="12"/>
            </svg>
            New Task
          </button>
          <button className="user-avatar-btn" onClick={() => navigate('/profile')} title="My Profile">
            <span className="user-avatar-icon">
              {user?.fullName?.charAt(0).toUpperCase() ?? '?'}
            </span>
            <span className="user-avatar-name">{user?.fullName}</span>
          </button>
          <ThemeToggle />
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      <div className="dashboard-inner">

        {/* ── Stats panel ── */}
        <div className="stats-panel">

          <div className="stat-card">
            <div className="stat-icon stat-icon-total">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <rect x="3" y="3" width="7" height="7" rx="1.5"/>
                <rect x="14" y="3" width="7" height="7" rx="1.5"/>
                <rect x="3" y="14" width="7" height="7" rx="1.5"/>
                <rect x="14" y="14" width="7" height="7" rx="1.5"/>
              </svg>
            </div>
            <div className="stat-info">
              <span className="stat-value">{stats.total}</span>
              <span className="stat-label">Total Tasks</span>
            </div>
          </div>

          <div className="stat-card pending">
            <div className="stat-icon stat-icon-pending">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="12" cy="12" r="9"/>
                <polyline points="12 7 12 12 15.5 15.5"/>
              </svg>
            </div>
            <div className="stat-info">
              <span className="stat-value">{stats.pending}</span>
              <span className="stat-label">Pending</span>
            </div>
          </div>

          <div className="stat-card inprogress">
            <div className="stat-icon stat-icon-inprogress">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <polyline points="22 12 18 12 15 21 9 3 6 12 2 12"/>
              </svg>
            </div>
            <div className="stat-info">
              <span className="stat-value">{stats.inProgress}</span>
              <span className="stat-label">In Progress</span>
            </div>
          </div>

          <div className="stat-card completed">
            <div className="stat-icon stat-icon-completed">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"/>
                <polyline points="22 4 12 14.01 9 11.01"/>
              </svg>
            </div>
            <div className="stat-info">
              <span className="stat-value">{stats.completed}</span>
              <span className="stat-label">Completed</span>
            </div>
          </div>

        </div>

        {/* ── Toolbar ── */}
        <div className="toolbar">
          <div className="search-box">
            <span className="search-icon">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
              </svg>
            </span>
            <input
              className="search-input"
              type="text"
              placeholder="Search tasks…"
              value={search}
              onChange={e => handleSearch(e.target.value)}
            />
            {search && (
              <button className="search-clear" onClick={() => handleSearch('')}>✕</button>
            )}
          </div>

          <div className="filter-group">
            {(['All', 'Pending', 'InProgress', 'Completed'] as const).map(s => (
              <button
                key={s}
                className={`filter-btn ${statusFilter === s ? 'active' : ''}`}
                onClick={() => handleFilterChange(s)}
              >
                {s === 'InProgress' ? 'In Progress' : s}
              </button>
            ))}
          </div>

          <div className="sort-group">
            <span>Sort:</span>
            <button className={`sort-btn ${sortOrder === 'asc' ? 'active' : ''}`} onClick={() => setSortOrder('asc')}>↑ Earliest</button>
            <button className={`sort-btn ${sortOrder === 'desc' ? 'active' : ''}`} onClick={() => setSortOrder('desc')}>↓ Latest</button>
          </div>

          <span className="task-count">{filtered.length} task{filtered.length !== 1 ? 's' : ''}</span>

          <button className="btn-export" onClick={handleExportCsv} disabled={exporting}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="13" height="13">
              <path d="M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4"/>
              <polyline points="7 10 12 15 17 10"/><line x1="12" y1="15" x2="12" y2="3"/>
            </svg>
            {exporting ? 'Exporting…' : 'Export CSV'}
          </button>
        </div>

        {/* ── Task list ── */}
        {loading ? (
          <div className="loading-state">
            <div className="spinner" />
            <p>Loading tasks…</p>
          </div>
        ) : (
          <TaskList
            tasks={paginated}
            onEdit={t => { setEditingTask(t); setShowForm(true); }}
            onDelete={handleDelete}
            showOwner={isAdmin}
          />
        )}

        {/* ── Pagination ── */}
        {totalPages > 1 && (
          <div className="pagination">
            <button disabled={page === 1} onClick={() => setPage(p => p - 1)}>← Prev</button>
            <span className="pagination-info">Page {page} of {totalPages}</span>
            <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>Next →</button>
          </div>
        )}

      </div>

      {/* ── Create / Edit modal ── */}
      {showForm && (
        <TaskForm
          initial={editingTask}
          onSubmit={editingTask ? handleUpdate : handleCreate}
          onCancel={() => { setShowForm(false); setEditingTask(undefined); }}
          loading={saving}
        />
      )}

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  );
}
