using Newtonsoft.Json;

namespace SonarJsConfig.Data
{
    public class TSConfigRequest
    {
        [JsonProperty("tsconfig")]
        public string TSConfigAbsoluteFilePath { get; set; }
    }
}
