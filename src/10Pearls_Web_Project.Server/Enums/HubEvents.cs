namespace _10Pearls_Web_Project.Server.Enums
{
    /// <summary>
    /// Single source of truth for all SignalR event names and group names.
    /// Never use raw strings — always reference these constants.
    /// Frontend mirror: src/types/hubEvents.ts
    /// </summary>
    public static class HubEvents
    {
        // ── Event names ────────────────────────────────────────────────────
        public const string TaskCreated       = "TaskCreated";
        public const string TaskUpdated       = "TaskUpdated";
        public const string TaskDeleted       = "TaskDeleted";
        public const string TaskStatusChanged = "TaskStatusChanged";

        // ── Group names ────────────────────────────────────────────────────
        // Admin group — all admin connections join this on connect
        public const string AdminGroup = "admin-group";

        // User group prefix — append userId to get the full group name
        private const string UserGroupPrefix = "user-";

        /// <summary>Returns the SignalR group name for a specific user.</summary>
        public static string UserGroup(string userId) => $"{UserGroupPrefix}{userId}";
    }
}
