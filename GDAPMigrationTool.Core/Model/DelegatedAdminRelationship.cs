using GBM.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace PartnerLed.Model
{
    /// <summary>
    /// This represents the partner's view of a Delegated Admin relationship between a partner and a customer.
    /// </summary>
    public class DelegatedAdminRelationship
    {
        /// <summary>
        /// Gets or sets the unique ID of the relationship. This is set by the system and cannot be set by the partner.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the Customer Delegated Admin relationship ID.
        /// </summary>
        public string CustomerDelegatedAdminRelationshipId { get; set; }

        /// <summary>
        /// Gets or sets the display name of the relationship. This is primarily meant for ease of identification. This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the details of the partner in the relationship. This is set by the partner and cannot be changed by the customer.
        /// TODO: Remove this.
        /// </summary>
        public DelegatedAdminRelationshipParticipant Partner { get; set; }

        /// <summary>
        /// Gets or sets the details of the customer in the relationship.
        /// </summary>
        public DelegatedAdminRelationshipCustomerParticipant Customer { get; set; }

        /// <summary>
        /// Gets or sets the access details for this relationship. This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public DelegatedAdminAccessDetails AccessDetails { get; set; }

        /// <summary>
        /// Gets or sets the total duration (in days) of the relationship in UTC (ISO 8601 format). This is set by the partner and cannot be changed by the customer.
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the status of the relationship. The customer *can* change this to indicate approval and perhaps in future, a termination request.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DelegatedAdminRelationshipStatus? Status { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this relationship was created in UTC (ISO 8601 format).
        /// </summary>
        public string CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this relationship became active in UTC (ISO 8601 format).
        /// </summary>
        public string ActivatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this relationship was last modified in UTC (ISO 8601 format).
        /// </summary>
        public string LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the date and time at which this relationship is scheduled to expire in UTC (ISO 8601 format).
        /// Essentially, EndDateTime = ActivatedDateTime + DurationInDays.
        /// </summary>
        public string EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the list of Delegated Admin relationship requests.
        /// </summary>
        [JsonIgnore]
        public IEnumerable<DelegatedAdminRelationshipRequest> Requests { get; set; }

        /// <summary>
        /// Gets or sets the list of Delegated Admin access assignments.
        /// </summary>
        public IEnumerable<DelegatedAdminAccessAssignment> AccessAssignments { get; set; }
    }
}

