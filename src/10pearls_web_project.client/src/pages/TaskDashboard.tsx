import { useEffect, useState, useMemo } from 'react';
import type { AppTaskStatus, CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { taskService, type TaskStats } from '../services/taskService';
import { useAuth } from '../context/AuthContext';
import { TaskList } from '../components/TaskList';
import { TaskForm } from '../components/TaskForm';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import { useNavigate } from 'react-router-dom';
import './TaskDashboard.css';

const PAGE_SIZE = 6;
type SortOrder = 'asc' | 'desc';

export function TaskDashboard() {
  const { user, isAdmin, logout } = useAuth();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [tasks, setTasks] = useState<Task[]>([]);
  const [stats, setStats] = useState<TaskStats | null>(null);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  const [statusFilter, setStatusFilter] = useState<AppTaskStatus | 'All'>('All');
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc');
  const [page, setPage] = useState(1);

  const [showForm, setShowForm] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | undefined>();

  useEffect(() => {
    loadAll();
  }, []);

  async function loadAll() {
    setLoading(true);
    try {
      const [data, statsData] = await Promise.all([
        taskService.getAll(),
        taskService.getStats(),
      ]);
      setTasks(data);
      setStats(statsData);
    } catch {
      show('Failed to load tasks', 'error');
    } finally {
      setLoading(false);
    }
  }

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
  const paginated = filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE);

  function handleFilterChange(value: AppTaskStatus | 'All') {
    setStatusFilter(value);
    setPage(1);
  }

  async function handleCreate(dto: CreateTaskDTO) {
    setSaving(true);
    try {
      const created = await taskService.create(dto);
      setTasks(prev => [created, ...prev]);
      setStats(prev => prev ? { ...prev, total: prev.total + 1, pending: prev.pending + 1 } : prev);
      setShowForm(false);
      show('Task created successfully');
    } finally {
      setSaving(false);
    }
  }

  async function handleUpdate(dto: CreateTaskDTO) {
    if (!editingTask) return;
    setSaving(true);
    try {
      const updateDto: UpdateTaskDTO = {
        title: dto.title,
        description: dto.description,
        dueDate: dto.dueDate,
        status: dto.status,
        priority: dto.priority,
      };
      const updated = await taskService.update(editingTask.id, updateDto);
      setTasks(prev => prev.map(t => t.id === updated.id ? updated : t));
      setEditingTask(undefined);
      setShowForm(false);
      // Refresh stats since status may have changed
      taskService.getStats().then(setStats).catch(() => null);
      show('Task updated successfully');
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: string) {
    if (!confirm('Delete this task?')) return;
    try {
      await taskService.delete(id);
      setTasks(prev => prev.filter(t => t.id !== id));
      taskService.getStats().then(setStats).catch(() => null);
      show('Task deleted');
    } catch {
      show('Failed to delete task', 'error');
    }
  }

  function handleLogout() {
    logout();
    navigate('/signin');
  }

  return (
    <div className="dashboard">
      {/* Header */}
      <header className="dashboard-header">
        <div className="header-title-group">
          <div className="title-row">
            <h1 className="dashboard-title">
              {isAdmin ? 'Admin Dashboard' : 'My Tasks'}
            </h1>
            {isAdmin && <span className="admin-badge">Admin</span>}
          </div>
          <p className="dashboard-subtitle">
            {isAdmin
              ? `Global view — all users' tasks · Welcome, ${user?.fullName}`
              : `Welcome back, ${user?.fullName}`}
          </p>
        </div>
        <div className="header-actions">
          <button className="btn-primary" onClick={() => { setEditingTask(undefined); setShowForm(true); }}>
            + New Task
          </button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      {/* Stats panel */}
      {stats && (
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
      )}

      {/* Toolbar */}
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
          <button className={`sort-btn ${sortOrder === 'asc' ? 'active' : ''}`} onClick={() => setSortOrder('asc')}>↑ Earliest</button>
          <button className={`sort-btn ${sortOrder === 'desc' ? 'active' : ''}`} onClick={() => setSortOrder('desc')}>↓ Latest</button>
        </div>
        <span className="task-count">{filtered.length} task{filtered.length !== 1 ? 's' : ''}</span>
      </div>

      {/* Task list */}
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

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={page === 1} onClick={() => setPage(p => p - 1)}>← Prev</button>
          <span>Page {page} of {totalPages}</span>
          <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>Next →</button>
        </div>
      )}

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
