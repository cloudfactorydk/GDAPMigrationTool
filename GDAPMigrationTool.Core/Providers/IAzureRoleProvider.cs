using GBM.Model;
using PartnerLed.Model;

namespace PartnerLed.Providers
{
    public interface IAzureRoleProvider
    {
        Task<bool> ExportAzureDirectoryRoles(ExportImport type);
    }
}
