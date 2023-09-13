namespace GDAPMigrationTool.Core.Providers
{
    public interface IExportImportProvider
    {
        Task WriteAsync<T>(IEnumerable<T>? data, string fileName);
        Task<List<T>> ReadAsync<T>(string fileName);
    }
}
