using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Providers
{
    public interface IGdapProvider
    {
        Task<List<DelegatedAdminRelationship>?> GetAllGDAPAsync(ExportImport type);

        Task<(List<DelegatedAdminRelationship>? successfulGDAP, List<DelegatedAdminRelationshipErrored>? failedGDAP)> CreateGDAPRequestAsync(
            ExportImport type,
            List<DelegatedAdminRelationshipRequest> delegatedAdminRelationshipRequests,
            List<UnifiedRole> unifiedRoles);

        Task<bool> RefreshGDAPRequestAsync(ExportImport type);

        Task<bool> TerminateGDAPRequestAsync(ExportImport type);

        Task<bool> CreateTerminateRelationshipFile(ExportImport type);
    }
}
