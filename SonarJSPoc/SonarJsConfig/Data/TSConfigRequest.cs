using Newtonsoft.Json;

namespace SonarJsConfig.ESLint.Data
{
    public class TSConfigRequest
    {
        [JsonProperty("tsconfig")]
        public string TSConfigAbsoluteFilePath { get; set; }
    }

    public class TSConfigResponse
    {
        [JsonProperty("files")]
        public string[] Files{ get; set; }

        [JsonProperty("projectReferences")]
        public string[] ProjectReferences { get; set; }

        [JsonProperty("error")]
        public string Error { get; set; }

        [JsonProperty("errorCode")]
        public ParsingErrorCode errorCode { get; set; }
    }
}
