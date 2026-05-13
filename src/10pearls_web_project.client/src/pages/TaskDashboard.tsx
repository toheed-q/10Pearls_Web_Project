import { useEffect, useState, useMemo } from 'react';
import type { AppTaskStatus, CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { taskService } from '../services/taskService';
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
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [tasks, setTasks] = useState<Task[]>([]);
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);

  // Filter + sort + pagination state
  const [statusFilter, setStatusFilter] = useState<AppTaskStatus | 'All'>('All');
  const [sortOrder, setSortOrder] = useState<SortOrder>('asc');
  const [page, setPage] = useState(1);

  // Modal state
  const [showForm, setShowForm] = useState(false);
  const [editingTask, setEditingTask] = useState<Task | undefined>();

  // Load tasks on mount
  useEffect(() => {
    loadTasks();
  }, []);

  async function loadTasks() {
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

  // Filter → sort → paginate (all in memory, no extra API calls)
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

  // Reset to page 1 when filter changes
  function handleFilterChange(value: AppTaskStatus | 'All') {
    setStatusFilter(value);
    setPage(1);
  }

  async function handleCreate(dto: CreateTaskDTO) {
    setSaving(true);
    try {
      const created = await taskService.create(dto);
      setTasks(prev => [created, ...prev]);
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
        <div>
          <h1 className="dashboard-title">My Tasks</h1>
          <p className="dashboard-subtitle">Welcome back, {user?.fullName}</p>
        </div>
        <div className="header-actions">
          <button className="btn-primary" onClick={() => { setEditingTask(undefined); setShowForm(true); }}>
            + New Task
          </button>
          <button className="btn-logout" onClick={handleLogout}>Logout</button>
        </div>
      </header>

      {/* Toolbar: filter + sort + count */}
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
          <span>Sort by due date:</span>
          <button
            className={`sort-btn ${sortOrder === 'asc' ? 'active' : ''}`}
            onClick={() => setSortOrder('asc')}
          >↑ Earliest</button>
          <button
            className={`sort-btn ${sortOrder === 'desc' ? 'active' : ''}`}
            onClick={() => setSortOrder('desc')}
          >↓ Latest</button>
        </div>
        <span className="task-count">{filtered.length} task{filtered.length !== 1 ? 's' : ''}</span>
      </div>

      {/* Task list */}
      {loading ? (
        <div className="loading-state">Loading tasks…</div>
      ) : (
        <TaskList tasks={paginated} onEdit={t => { setEditingTask(t); setShowForm(true); }} onDelete={handleDelete} />
      )}

      {/* Pagination */}
      {totalPages > 1 && (
        <div className="pagination">
          <button disabled={page === 1} onClick={() => setPage(p => p - 1)}>← Prev</button>
          <span>Page {page} of {totalPages}</span>
          <button disabled={page === totalPages} onClick={() => setPage(p => p + 1)}>Next →</button>
        </div>
      )}

      {/* Create / Edit modal */}
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
