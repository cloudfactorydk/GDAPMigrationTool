using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Providers
{
    public interface IAzureRoleProvider
    {
        Task<bool> ExportAzureDirectoryRoles(ExportImport type);
    }
}
