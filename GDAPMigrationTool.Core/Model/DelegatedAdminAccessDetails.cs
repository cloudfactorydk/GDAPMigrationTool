namespace GDAPMigrationTool.Core.Model
{
    public class DelegatedAdminAccessDetails
    {
        /// <summary>
        /// Gets or sets the list of unified roles to be granted to partner agents for access.
        /// </summary>
        public IEnumerable<UnifiedRole> UnifiedRoles { get; set; }
    }
}