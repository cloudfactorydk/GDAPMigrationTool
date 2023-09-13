using Newtonsoft.Json;

namespace GDAPMigrationTool.Core.Model
{
    public class Attribute
    {
        [JsonProperty("objectType")]
        public string objectType { get; set; }
    }
}
