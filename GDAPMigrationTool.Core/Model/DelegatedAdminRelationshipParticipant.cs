namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// Contains details of a participant in a Delegated Admin relationship.
    /// </summary>
    public class DelegatedAdminRelationshipParticipant
    {
        /// <summary>
        /// Gets or sets the tenant ID of the participant in the relationship.
        /// </summary>
        public string TenantId { get; set; }
    }
}