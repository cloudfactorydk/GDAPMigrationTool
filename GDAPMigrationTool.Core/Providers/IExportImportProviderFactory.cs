using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Providers
{
    public interface IExportImportProviderFactory
    {
        IExportImportProvider Create(ExportImport type);
    }
}