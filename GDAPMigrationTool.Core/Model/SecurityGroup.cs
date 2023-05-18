using Newtonsoft.Json;

namespace PartnerLed.Model
{
    /// <summary>
    /// Specifies the details of a security group.
    /// </summary>
    public class SecurityGroup
    {
        /// <summary>
        /// Gets or sets the unique ID (Role template ID) of security group.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the display name of security group.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the AD roles guids in comma separated format.
        /// </summary>
        public string CommaSeperatedRoles { get; set; }

        /// <summary>
        ///  Gets the List of Unified roles.
        /// </summary>
        [JsonProperty("Roles", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IEnumerable<UnifiedRole?>? Roles
        {
            get
            {
                return CommaSeperatedRoles != null ? CommaSeperatedRoles.Split(new char[] { ',' }).Select(r => new UnifiedRole() { RoleDefinitionId = r }).GroupBy(i => i.RoleDefinitionId).Select(g => g.FirstOrDefault()) : null;
            }
        }
    }
}