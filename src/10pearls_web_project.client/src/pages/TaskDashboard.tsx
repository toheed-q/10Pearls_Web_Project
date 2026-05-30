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
import './TaskDashboard.css';

const PAGE_SIZE = 6;
type SortOrder = 'asc' | 'desc';

// Derive stats from task array — always in sync with real-time updates, no extra API call
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

  const [tasks, setTasks]     = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving]   = useState(false);

  const [statusFilter, setStatusFilter] = useState<AppTaskStatus | 'All'>('All');
  const [sortOrder, setSortOrder]       = useState<SortOrder>('asc');
  const [page, setPage]                 = useState(1);

  const [showForm, setShowForm]       = useState(false);
  const [editingTask, setEditingTask] = useState<Task | undefined>();

  // ── SignalR real-time handlers ───────────────────────────────────────────
  // useCallback ensures the same function reference is used for cleanup in useTaskHub
  const onTaskCreated = useCallback((task: Task) => {
    setTasks(prev => {
      // Deduplicate — own optimistic add already inserted it
      if (prev.some(t => t.id === task.id)) return prev;
      return [task, ...prev];
    });
  }, []);

  const onTaskUpdated = useCallback((task: Task) => {
    setTasks(prev => prev.map(t => t.id === task.id ? task : t));
  }, []);

  const onTaskDeleted = useCallback((taskId: string) => {
    setTasks(prev => prev.filter(t => t.id !== taskId));
  }, []);

  const onTaskStatusChanged = useCallback((task: Task) => {
    setTasks(prev => prev.map(t => t.id === task.id ? task : t));
  }, []);

  const { connectionState } = useTaskHub({
    onTaskCreated,
    onTaskUpdated,
    onTaskDeleted,
    onTaskStatusChanged,
  });

  // ── Stats derived from live task list — no separate API call needed ──────
  const stats = useMemo(() => deriveStats(tasks), [tasks]);

  // ── Initial data load ────────────────────────────────────────────────────
  useEffect(() => { loadAll(); }, []);

  async function loadAll() {
    setLoading(true);
    try {
      const data = await taskService.getAll();
      setTasks(data);
    } catch {
      show('Failed to load tasks', 'error');
    } finally {
      setLoading(false);
    }
  }

  // ── Filter / sort / paginate ─────────────────────────────────────────────
  const filtered = useMemo(() => {
    let result = statusFilter === 'All'
      ? tasks
      : tasks.filter(t => t.status === statusFilter);

    result = [...result].sort((a, b) => {
      const diff = new Date(a.dueDate).getTime() - new Date(b.dueDate).getTime();
      return sortOrder === 'asc' ? diff : -diff;
    });

    return result;
  }, [tasks, statusFilter, sortOrder]);

  const totalPages = Math.max(1, Math.ceil(filtered.length / PAGE_SIZE));
  const paginated  = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  function handleFilterChange(value: AppTaskStatus | 'All') {
    setStatusFilter(value);
    setPage(1);
  }

  // ── CRUD handlers ────────────────────────────────────────────────────────
  async function handleCreate(dto: CreateTaskDTO) {
    setSaving(true);
    try {
      const created = await taskService.create(dto);
      // Optimistic insert — onTaskCreated will deduplicate if SignalR fires too
      setTasks(prev => [created, ...prev]);
      setShowForm(false);
      show('Task created');
    } finally {
      setSaving(false);
    }
  }

  async function handleUpdate(dto: CreateTaskDTO) {
    if (!editingTask) return;
    setSaving(true);
    try {
      const updateDto: UpdateTaskDTO = {
        title:       dto.title,
        description: dto.description,
        dueDate:     dto.dueDate,
        status:      dto.status,
        priority:    dto.priority,
      };
      const updated = await taskService.update(editingTask.id, updateDto);
      setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
      setEditingTask(undefined);
      setShowForm(false);
      show('Task updated');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this task?')) return;
    try {
      await taskService.delete(id);
      setTasks(prev => prev.filter(t => t.id !== id));
      show('Task deleted');
    } catch {
      show('Failed to delete task', 'error');
    }
  }

  function handleLogout() {
    logout();
    navigate('/signin');
  }

  // ── Connection indicator ─────────────────────────────────────────────────
  const connLabel =
    connectionState === signalR.HubConnectionState.Connected    ? 'Live'         :
    connectionState === signalR.HubConnectionState.Reconnecting ? 'Reconnecting' :
    'Offline';

  const connClass =
    connectionState === signalR.HubConnectionState.Connected    ? 'conn-live'         :
    connectionState === signalR.HubConnectionState.Reconnecting ? 'conn-reconnecting' :
    'conn-offline';

  return (
    <div className="dashboard">

      {/* ── Header ── */}
      <header className="dashboard-header">
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
            {isAdmin
              ? `Global view — all users' tasks · Welcome, ${user?.fullName}`
              : `Welcome back, ${user?.fullName}`}
          </p>
        </div>
        <div className="header-actions">
          <button className="btn-primary"
            onClick={() => { setEditingTask(undefined); setShowForm(true); }}>
            + New Task
          </button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      {/* ── Stats panel — live, derived from task list ── */}
      <div className="stats-panel">
        <div className="stat-card">
          <span className="stat-value">{stats.total}</span>
          <span className="stat-label">Total</span>
        </div>
        <div className="stat-card pending">
          <span className="stat-value">{stats.pending}</span>
          <span className="stat-label">Pending</span>
        </div>
        <div className="stat-card inprogress">
          <span className="stat-value">{stats.inProgress}</span>
          <span className="stat-label">In Progress</span>
        </div>
        <div className="stat-card completed">
          <span className="stat-value">{stats.completed}</span>
          <span className="stat-label">Completed</span>
        </div>
      </div>

      {/* ── Toolbar ── */}
      <div className="toolbar">
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
          <button className={`sort-btn ${sortOrder === 'asc' ? 'active' : ''}`}
            onClick={() => setSortOrder('asc')}>↑ Earliest</button>
          <button className={`sort-btn ${sortOrder === 'desc' ? 'active' : ''}`}
            onClick={() => setSortOrder('desc')}>↓ Latest</button>
        </div>
        <span className="task-count">
          {filtered.length} task{filtered.length !== 1 ? 's' : ''}
        </span>
      </div>

      {/* ── Task list ── */}
      {loading ? (
        <div className="loading-state">Loading tasks…</div>
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
          <span>Page {page} of {totalPages}</span>
          <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>Next →</button>
        </div>
      )}

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
