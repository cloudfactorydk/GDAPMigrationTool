namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// Contains details of a customer participant in a Delegated Admin relationship.
    /// </summary>
    public class DelegatedAdminRelationshipCustomerParticipant : DelegatedAdminRelationshipParticipant
    {
        /// <summary>
        /// Gets or sets the display name of the customer's organization.
        /// </summary>
        public string DisplayName { get; set; }
    }
}
