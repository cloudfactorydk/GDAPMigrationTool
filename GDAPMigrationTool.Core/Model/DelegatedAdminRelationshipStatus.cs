using System.Runtime.Serialization;

namespace GDAPMigrationTool.Core.Model
{
    /// <summary>
    /// Contains the various statuses of a Delegated Admin relationship.
    /// The statuses are defined in lexicographic order so that the relationships can be sorted on the basis of status using OData orderBy filter.
    /// Please add new statuses in similar lexicographic order.
    /// TODO: Find a way to sort the relationships on status enum names instead of enum values using OData orderBy filter.
    /// </summary>
    public enum DelegatedAdminRelationshipStatus
    {
        /// <summary>
        /// A Delegated Admin relationship moves to this status when the relationship is getting provisioned after approval.
        /// </summary>
        [EnumMember(Value = "activating")]
        Activating,

        /// <summary>
        /// A Delegated Admin relationship moves to this status after provisioning is complete.
        /// </summary>
        [EnumMember(Value = "active")]
        Active,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when the approval is pending from the customer.
        /// </summary>
        [EnumMember(Value = "approvalPending")]
        ApprovalPending,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when the customer approves the request.
        /// </summary>
        [EnumMember(Value = "approved")]
        Approved,

        /// <summary>
        /// A Delegated Admin relationship is in a created status after creation until the time an approval is requested from a customer.
        /// </summary>
        [EnumMember(Value = "created")]
        Created,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when the relationship is completely expired.
        /// </summary>
        [EnumMember(Value = "expired")]
        Expired,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when the duration of the relationship is complete and the expiration process is kicked off.
        /// </summary>
        [EnumMember(Value = "expiring")]
        Expiring,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when the relationship is completely terminated.
        /// </summary>
        [EnumMember(Value = "terminated")]
        Terminated,

        /// <summary>
        /// A Delegated Admin relationship moves to this status when a termination request is being processed.
        /// </summary>
        [EnumMember(Value = "terminating")]
        Terminating,

        /// <summary>
        /// A Delegated Admin relationship moves to this status if termination is requested either by the partner or the customer.
        /// </summary>
        [EnumMember(Value = "terminationRequested")]
        TerminationRequested,

        /// <summary>
        /// Evolvable enumerations sentinel value. Do not use. Doc: https://dev.azure.com/msazure/One/_wiki/wikis/Microsoft%20Graph%20Partners/103144/Evolvable-enums
        /// </summary>
        [EnumMember(Value = "unknownFutureValue")]
        UnknownFutureValue,
    }
}
