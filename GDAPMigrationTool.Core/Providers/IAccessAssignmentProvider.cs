using GDAPMigrationTool.Core.Model;

namespace GDAPMigrationTool.Core.Providers
{
    public interface IAccessAssignmentProvider
    {
        Task<List<SecurityGroup?>?> ExportSecurityGroup(ExportImport type);

        Task<(IEnumerable<DelegatedAdminAccessAssignmentRequest> successfulAccessAssignment, IEnumerable<DelegatedAdminAccessAssignmentRequest> failedAccessAssignment)>
            CreateAccessAssignmentRequestAsync(
                ExportImport type,
                List<SecurityGroup> securityGroups,
                List<ADRole> rolesForSecurityGroup,
                List<DelegatedAdminRelationship> delegatedAdminRelationships);

        Task<bool> RefreshAccessAssignmentRequest(ExportImport type);

        Task<List<DelegatedAdminAccessAssignmentRequest>?> UpdateAccessAssignmentRequestAsync(
            ExportImport type,
            List<ADRole> unifiedRoles,
            SecurityGroup securityGroup,
            IEnumerable<DelegatedAdminAccessAssignmentRequest> accessAssignmentList);

        Task<bool> DeleteAccessAssignmentRequestAsync(ExportImport type);

        Task<bool> CreateDeleteAccessAssignmentFile(ExportImport type);


        Task<DelegatedAdminAccessAssignmentRequest?> PostGranularAdminAccessAssignment(
            DelegatedAdminRelationship gdapRelationship,
            DelegatedAdminAccessAssignment data,
            IEnumerable<SecurityGroup> SecurityGroupList);
    }
}