namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// This represents the partner's view of a Granular Admin relationship between a partner and a customer for Request.
    /// </summary>
    public class DelegatedAdminRelationshipRequest
    {
        /// <summary>
        /// Gets or sets the name of the relationship. This is primarily meant for ease of identification. This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the details of the partner in the relationship. This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public string PartnerTenantId { get; set; }

        /// <summary>
        /// Gets or sets the details of the customer in the relationship.
        /// </summary>
        public string CustomerTenantId { get; set; }


        /// <summary>
        /// Gets or sets the details of the customer in the relationship.
        /// </summary>
        public string OrganizationDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the total duration (in days) of the relationship. This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public string Duration { get; set; }

    }

}
