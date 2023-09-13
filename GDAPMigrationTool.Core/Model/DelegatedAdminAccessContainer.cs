using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace GDAPMigrationTool.Core.Model
{
    public class DelegatedAdminAccessContainer
    {
        /// <summary>
        /// Gets or sets the ID of the access container (e.g.: security group ID).
        /// </summary>
        public string AccessContainerId { get; set; }

        /// <summary>
        /// Gets or sets the type of the access container.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public DelegatedAdminAccessContainerType? AccessContainerType { get; set; }
    }
}