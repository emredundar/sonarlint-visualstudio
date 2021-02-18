using System.Collections.Generic;
using Newtonsoft.Json;

namespace SonarJsConfig.Data
{
    public class EslintBridgeAnalysisRequest
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }
        [JsonProperty("fileContent")]
        public string FileContent { get; set; }
        [JsonProperty("rules")]
        public EsLintRuleConfig[] Rules { get; set; }

    }

    public class EsLintRuleConfig
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("configurations")]
        public string[] Configurations { get; set; }
    }


    public class EslintBridgeResponse
    {
        [JsonProperty("issues")]
        public IEnumerable<EslintBridgeIssue> Issues { get; set; }

        [JsonProperty("parsingError")]
        public EslintBridgeParsingError EslintBridgeParsingError { get; set; }
    }

    public class EslintBridgeParsingError
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("code")]
        public string ErrorCode { get; set; }
    }


    public class EslintBridgeIssue
    {
        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("endColumn")]
        public int? EndColumn { get; set; }

        [JsonProperty("endLine")]
        public int? EndLine { get; set; }

        [JsonProperty("ruleId")]
        public string RuleId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("cost")]
        public int? Cost { get; set; }
    }
}
