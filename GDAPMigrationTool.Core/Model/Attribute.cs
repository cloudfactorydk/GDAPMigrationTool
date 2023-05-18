using Newtonsoft.Json;

namespace PartnerLed.Model
{
    public class Attribute
    {
        [JsonProperty("objectType")]
        public string objectType { get; set; }
    }
}
