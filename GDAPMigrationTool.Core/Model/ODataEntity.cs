
using Newtonsoft.Json;

namespace GDAPMigrationTool.Core.Model
{

    public class ODataEntity
    {
        [JsonProperty(PropertyName = "@odata.etag")]
        public string ETag { get; set; }
    }

}
