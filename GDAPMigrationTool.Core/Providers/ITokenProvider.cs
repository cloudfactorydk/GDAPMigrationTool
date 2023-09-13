using GDAPMigrationTool.Core.Model;
using Microsoft.Identity.Client;

namespace GDAPMigrationTool.Core.Providers
{
    public interface ITokenProvider
    {
        Task<AuthenticationResult?> GetTokenAsync(Resource type);
        Task<bool> CheckPrerequisite();
        string getPartnertenantId();
    }
}
