

namespace GDAPMigrationTool.Core.Model
{
    public class DAPTerminate
    {
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

    }
}
