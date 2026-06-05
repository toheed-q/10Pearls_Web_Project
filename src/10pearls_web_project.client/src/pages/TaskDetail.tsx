import { useEffect, useState, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { taskService } from '../services/taskService';
import { taskHub } from '../services/taskHub';
import { HubEvents } from '../types/hubEvents';
import { TaskForm } from '../components/TaskForm';
import { ToastContainer } from '../components/ToastContainer';
import { useToast } from '../hooks/useToast';
import type { AppTaskStatus, CreateTaskDTO, Task, UpdateTaskDTO } from '../types/task';
import { ThemeToggle } from '../components/ThemeToggle';
import './TaskDetail.css';

const STATUS_LABELS: Record<AppTaskStatus, string> = {
  Pending:    'Pending',
  InProgress: 'In Progress',
  Completed:  'Completed',
};

const ALL_STATUSES: AppTaskStatus[] = ['Pending', 'InProgress', 'Completed'];

export function TaskDetail() {
  const { id }   = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { toasts, show, dismiss } = useToast();

  const [task, setTask]         = useState<Task | null>(null);
  const [loading, setLoading]   = useState(true);
  const [notFound, setNotFound] = useState(false);
  const [showEdit, setShowEdit] = useState(false);
  const [saving, setSaving]     = useState(false);

  useEffect(() => { if (!id) return; fetchTask(); }, [id]);

  async function fetchTask() {
    setLoading(true);
    try { const data = await taskService.getById(id!); setTask(data); }
    catch { setNotFound(true); }
    finally { setLoading(false); }
  }

  const onTaskUpdated = useCallback((updated: Task) => {
    if (updated.id === id) setTask(updated);
  }, [id]);

  const onTaskStatusChanged = useCallback((updated: Task) => {
    if (updated.id === id) setTask(updated);
  }, [id]);

  const onTaskDeleted = useCallback((deletedId: string) => {
    if (deletedId === id) { show('Task was deleted', 'error'); setTimeout(() => navigate('/'), 1500); }
  }, [id, navigate, show]);

  useEffect(() => {
    taskHub.on(HubEvents.TaskUpdated,       onTaskUpdated);
    taskHub.on(HubEvents.TaskStatusChanged, onTaskStatusChanged);
    taskHub.on(HubEvents.TaskDeleted,       onTaskDeleted);
    return () => {
      taskHub.off(HubEvents.TaskUpdated,       onTaskUpdated);
      taskHub.off(HubEvents.TaskStatusChanged, onTaskStatusChanged);
      taskHub.off(HubEvents.TaskDeleted,       onTaskDeleted);
    };
  }, [onTaskUpdated, onTaskStatusChanged, onTaskDeleted]);

  async function handleStatusChange(newStatus: AppTaskStatus) {
    if (!task || newStatus === task.status) return;
    try {
      const updated = await taskService.update(task.id, { status: newStatus });
      setTask(updated); show('Status updated');
    } catch { show('Failed to update status', 'error'); }
  }

  async function handleEdit(dto: CreateTaskDTO) {
    if (!task) return;
    setSaving(true);
    try {
      const updateDto: UpdateTaskDTO = {
        title: dto.title, description: dto.description,
        dueDate: dto.dueDate, status: dto.status, priority: dto.priority,
      };
      const updated = await taskService.update(task.id, updateDto);
      setTask(updated); setShowEdit(false); show('Task updated');
    } finally { setSaving(false); }
  }

  async function handleDelete() {
    if (!task || !confirm('Delete this task? This cannot be undone.')) return;
    try { await taskService.delete(task.id); navigate('/'); }
    catch { show('Failed to delete task', 'error'); }
  }

  const isOverdue = task
    ? task.status !== 'Completed' && new Date(task.dueDate) < new Date()
    : false;

  if (loading) {
    return (
      <div className="td-page">
        <div className="loading-state"><div className="spinner" /><p>Loading task…</p></div>
      </div>
    );
  }

  if (notFound || !task) {
    return (
      <div className="td-page">
        <div className="td-not-found">
          <div className="td-not-found-icon">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round">
              <circle cx="11" cy="11" r="8"/><line x1="21" y1="21" x2="16.65" y2="16.65"/>
            </svg>
          </div>
          <h2>Task not found</h2>
          <p>This task doesn't exist or you don't have access to it.</p>
          <Link to="/" className="btn-primary td-back-btn">← Back to Dashboard</Link>
        </div>
      </div>
    );
  }

  return (
    <div className="td-page">

      <div className="td-top-row">
        <Link to="/" className="td-back-link">
          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
            <polyline points="15 18 9 12 15 6"/>
          </svg>
          Back to Dashboard
        </Link>
        <ThemeToggle />
      </div>

      <div className="td-card">

        <div className="td-header">
          <div className="td-badges">
            <span className={`badge priority-${task.priority.toLowerCase()}`}>{task.priority}</span>
            <span className={`badge status-${task.status.toLowerCase()}`}>{STATUS_LABELS[task.status]}</span>
            {isOverdue && <span className="badge badge-overdue">Overdue</span>}
          </div>
          <h1 className="td-title">{task.title}</h1>
        </div>

        {task.description && (
          <div className="td-section">
            <span className="td-section-label">Description</span>
            <p className="td-description">{task.description}</p>
          </div>
        )}

        <div className="td-meta-grid">
          <div className="td-meta-item">
            <span className="td-meta-label">Due Date</span>
            <span className={`td-meta-value ${isOverdue ? 'overdue' : ''}`}>
              {new Date(task.dueDate).toLocaleDateString('en-US', { year: 'numeric', month: 'long', day: 'numeric' })}
            </span>
          </div>
          <div className="td-meta-item">
            <span className="td-meta-label">Created</span>
            <span className="td-meta-value">
              {new Date(task.createdAt).toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
          <div className="td-meta-item">
            <span className="td-meta-label">Last Updated</span>
            <span className="td-meta-value">
              {new Date(task.updatedAt).toLocaleString('en-US', { dateStyle: 'medium', timeStyle: 'short' })}
            </span>
          </div>
        </div>

        <div className="td-section">
          <span className="td-section-label">Change Status</span>
          <div className="td-status-group">
            {ALL_STATUSES.map(s => (
              <button
                key={s}
                className={`td-status-btn ${task.status === s ? 'active' : ''} status-${s.toLowerCase()}`}
                onClick={() => handleStatusChange(s)}
                disabled={task.status === s}
              >
                {STATUS_LABELS[s]}
              </button>
            ))}
          </div>
        </div>

        <div className="td-actions">
          <button className="btn-primary" onClick={() => setShowEdit(true)}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
              <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"/>
              <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"/>
            </svg>
            Edit Task
          </button>
          <button className="td-btn-delete" onClick={handleDelete}>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" width="14" height="14">
              <polyline points="3 6 5 6 21 6"/><path d="M19 6l-1 14a2 2 0 0 1-2 2H8a2 2 0 0 1-2-2L5 6"/>
              <path d="M10 11v6"/><path d="M14 11v6"/><path d="M9 6V4a1 1 0 0 1 1-1h4a1 1 0 0 1 1 1v2"/>
            </svg>
            Delete
          </button>
        </div>

      </div>

      {showEdit && (
        <TaskForm
          initial={task}
          onSubmit={handleEdit}
          onCancel={() => setShowEdit(false)}
          loading={saving}
        />
      )}

      <ToastContainer toasts={toasts} onDismiss={dismiss} />
    </div>
  );
}
