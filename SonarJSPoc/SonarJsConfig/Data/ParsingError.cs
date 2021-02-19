using Newtonsoft.Json;

namespace SonarJsConfig.ESLint.Data
{
    public class ParsingError
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string ErrorCode { get; set; }
    }
}
