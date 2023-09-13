using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Providers
{
    public interface ICustomerProvider
    {
        Task<bool> DAPTermination(ExportImport type);

        Task<bool> CreateDAPTerminateFile(ExportImport type);
    }
}