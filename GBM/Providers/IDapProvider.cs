using Microsoft.Identity.Client;
using PartnerLed.Model;

namespace PartnerLed.Providers
{
    public interface IDapProvider
    {
        Task<List<DelegatedAdminRelationshipRequest>?> ExportCustomerDetails(ExportImport type);

        Task<bool> ExportCustomerBulk();

        Task<bool> GenerateDAPRelatioshipwithAccessAssignment(ExportImport type, List<DelegatedAdminRelationshipRequest> customers, List<UnifiedRole> roles);

        Task<AuthenticationResult> getTokenRaw(Resource resource);
    }
}