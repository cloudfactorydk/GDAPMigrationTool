using Newtonsoft.Json;

namespace PartnerLed.Model
{
    public class DAPRemovalInfo
    {
        [JsonProperty("allowDelegatedAccess")]
        public bool AllowDelegatedAccess { get; set; }

        [JsonProperty("attribute")]
        public Attribute attributes { get; set; }
    }
}
