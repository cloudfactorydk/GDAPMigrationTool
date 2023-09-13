namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// Specifies the details of a role.
    /// </summary>
    public class UnifiedRole
    {
        /// <summary>
        /// Gets or sets the unique ID of a unified role definition. https://docs.microsoft.com/en-us/graph/api/resources/unifiedRoleDefinition?view=graph-rest-1.0
        /// For AAD directory roles, this is the role template ID (https://docs.microsoft.com/en-us/azure/active-directory/users-groups-roles/directory-assign-admin-roles#role-template-ids).
        /// </summary>
        public string RoleDefinitionId { get; set; }
    }
}