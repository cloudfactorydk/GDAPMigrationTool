
using Newtonsoft.Json;

namespace GBM.Model
{

    public class ODataEntity
    {
        [JsonProperty(PropertyName = "@odata.etag")]
        public string ETag { get; set; }
    }

}
