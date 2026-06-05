namespace _10Pearls_Web_Project.Server.Services
{
    public interface ITaskExportService
    {
        Task<(byte[] Content, string FileName)> ExportCsvAsync(string userId, bool isAdmin);
    }
}
