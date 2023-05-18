using Newtonsoft.Json;

namespace PartnerLed.Model
{
    /// <summary>
    /// 
    /// </summary>
    public class DelegatedAdminAccessAssignmentRequest
    {
        /// <summary>
        /// Gets or sets the ID of the GDAP relationship
        /// </summary>
        public string GdapRelationshipId { get; set; }

        /// <summary>
        /// Gets or sets the details of the customer in the relationship.
        /// </summary>
        public DelegatedAdminRelationshipCustomerParticipant Customer { get; set; }

        /// <summary>
        /// Gets or sets the ID of the access assignment.
        /// </summary>
        public string AccessAssignmentId { get; set; }

        /// <summary>
        /// Gets or sets the access container.
        /// </summary>
        public string SecurityGroupId { get; set; }

        /// <summary>
        /// Gets or sets the access container name.
        /// </summary>
        public string SecurityGroupName { get; set; }

        /// <summary>
        /// Gets or sets the AD roles GUIDS in comma separated format.
        /// </summary>
        public string CommaSeperatedRoles { get; set; }

        /// <summary>
        /// Gets or sets the status of the assignment.
        /// </summary>
        public string Status { get; set; }

        /// <summary>
        /// Gets or sets the ETAG of the assignment.
        /// </summary>
        public string Etag { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this access assignment was created in UTC (ISO 8601 format).
        /// </summary>
        public string CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this access assignment was last modified in UTC (ISO 8601 format).
        /// </summary>
        public string LastModifiedDateTime { get; set; }

        /// <summary>
        ///  Gets the List of Unified roles.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<UnifiedRole?>? Roles
        {
            get
            {
                return CommaSeperatedRoles != null ? CommaSeperatedRoles.Split(new char[] { ',' }).Select(r => new UnifiedRole() { RoleDefinitionId = r }).GroupBy(i => i.RoleDefinitionId).Select(g => g.FirstOrDefault()) : null;
            }
        }
    }
}
