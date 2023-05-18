using System.Runtime.Serialization;

namespace PartnerLed.Model
{
    /// <summary>
    /// The type of access container in a Delegated Admin relationship.
    /// </summary>
    public enum DelegatedAdminAccessContainerType
    {
        /// <summary>
        /// Security group.
        /// </summary>
        [EnumMember(Value = "securityGroup")]
        SecurityGroup,

        /// <summary>
        /// Evolvable enumerations sentinel value. Do not use. Doc: https://dev.azure.com/msazure/One/_wiki/wikis/Microsoft%20Graph%20Partners/103144/Evolvable-enums
        /// </summary>
        [EnumMember(Value = "unknownFutureValue")]
        UnknownFutureValue,
    }
}