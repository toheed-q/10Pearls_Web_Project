export interface ChatEntry {
  keywords: string[];
  answer: string;
}

export const FAQ: ChatEntry[] = [
  {
    keywords: ['hello', 'hi', 'hey', 'start', 'help'],
    answer: "👋 Hi! I'm your Task Manager assistant. Ask me anything about using the app — creating tasks, managing roles, exporting data, and more.",
  },
  {
    keywords: ['create', 'add', 'new task'],
    answer: "➕ Click the **+ New Task** button in the top-right of the dashboard. Fill in the title, description, due date, priority, and status, then click **Save**.",
  },
  {
    keywords: ['edit', 'update', 'modify', 'change task'],
    answer: "✏️ Click the **Edit** button on any task card, update the fields, then click **Save Changes**.",
  },
  {
    keywords: ['delete', 'remove task'],
    answer: "🗑️ Click the **Delete** button on the task card and confirm. This action cannot be undone.",
  },
  {
    keywords: ['view', 'detail', 'task detail', 'open task'],
    answer: "🔍 Click the **View** button on any task card to open the full detail page where you can change status, edit, or delete the task.",
  },
  {
    keywords: ['status', 'pending', 'in progress', 'completed', 'change status'],
    answer: "🔄 Task statuses are **Pending**, **In Progress**, and **Completed**. Change the status from the task detail page or via the edit form.",
  },
  {
    keywords: ['priority', 'low', 'medium', 'high'],
    answer: "⚡ Tasks have three priority levels: **Low**, **Medium**, and **High**. Set the priority when creating or editing a task.",
  },
  {
    keywords: ['due date', 'overdue', 'deadline'],
    answer: "📅 Each task has a due date. Tasks not completed by their due date are marked **Overdue** in red.",
  },
  {
    keywords: ['filter', 'search', 'find task'],
    answer: "🔎 Use the **search bar** in the toolbar to instantly filter tasks by title. Use the **status filter buttons** to narrow by Pending, In Progress, or Completed.",
  },
  {
    keywords: ['sort', 'order', 'earliest', 'latest'],
    answer: "↕️ Use the **Sort** buttons in the toolbar — **↑ Earliest** shows soonest due first, **↓ Latest** shows furthest due first.",
  },
  {
    keywords: ['export', 'csv', 'download', 'excel'],
    answer: "📥 Click **↓ Export CSV** in the toolbar to download your tasks as a CSV file. Opens in Excel, Google Sheets, or LibreOffice. Admins export all tasks; users export only their own.",
  },
  {
    keywords: ['profile', 'account', 'my account', 'avatar'],
    answer: "👤 Click your **avatar icon** (top-right of the dashboard) to open your profile page where you can update your name and password.",
  },
  {
    keywords: ['password', 'change password', 'update password'],
    answer: "🔐 Go to your **Profile** page, enter your current password and new password in the Change Password section, then click **Save Changes**.",
  },
  {
    keywords: ['name', 'full name', 'update name', 'change name'],
    answer: "✍️ Go to your **Profile** page, update the **Full Name** field, then click **Save Changes**.",
  },
  {
    keywords: ['role', 'admin', 'user role', 'permission'],
    answer: "🛡️ Two roles exist:\n• **User** — manages their own tasks.\n• **Admin** — manages all users' tasks, accesses the Admin Dashboard, and promotes/demotes users.",
  },
  {
    keywords: ['promote', 'demote', 'make admin'],
    answer: "⬆️ Admins can promote or demote users from the **Manage Users** page (visible in the header for Admins only).",
  },
  {
    keywords: ['admin dashboard', 'manage users', 'user management'],
    answer: "🖥️ Admins see a **Manage Users** button in the header. It shows all users, their roles, task counts, and promote/demote actions.",
  },
  {
    keywords: ['real time', 'live', 'signalr', 'automatic', 'refresh'],
    answer: "⚡ The dashboard updates in **real time**. Task changes appear instantly for all connected users. The **Live** badge in the header shows your connection status.",
  },
  {
    keywords: ['login', 'sign in', 'signin'],
    answer: "🔑 Go to the **Sign In** page and enter your email and password. No account yet? Click **Sign Up**.",
  },
  {
    keywords: ['register', 'sign up', 'signup', 'create account'],
    answer: "📝 Click **Sign Up**, enter your full name, email, and password. You'll get the **User** role by default.",
  },
  {
    keywords: ['logout', 'sign out', 'log out'],
    answer: "👋 Click the **Logout** button in the top-right corner to sign out.",
  },
  {
    keywords: ['stats', 'statistics', 'count', 'total'],
    answer: "📊 The **stats panel** at the top shows Total, Pending, In Progress, and Completed task counts — updated live as tasks change.",
  },
  // fallback — must be last
  {
    keywords: [],
    answer: "🤔 I'm not sure about that. Try asking about: **tasks**, **status**, **priority**, **export**, **profile**, **roles**, or **real-time updates**.",
  },
];

export function getAnswer(input: string): string {
  const q = input.toLowerCase().trim();
  for (const entry of FAQ.slice(0, -1)) {
    if (entry.keywords.some(k => q.includes(k))) return entry.answer;
  }
  return FAQ[FAQ.length - 1].answer;
}

export const SUGGESTED_QUESTIONS = [
  'How do I create a task?',
  'How do I change task status?',
  'How do I export tasks?',
  'What is the Admin role?',
  'How do I update my profile?',
];
