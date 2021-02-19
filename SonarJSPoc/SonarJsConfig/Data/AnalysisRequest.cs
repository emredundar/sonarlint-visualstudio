using System.Collections.Generic;
using Newtonsoft.Json;

namespace SonarJsConfig.ESLint.Data
{
    public class AnalysisRequest
    {
        [JsonProperty("filePath")]
        public string FilePath { get; set; }

        [JsonProperty("fileContent")]
        public string FileContent { get; set; }

        [JsonProperty("ignoreHeaderComments")]
        public bool IgnoreHeaderComments { get; set; }

        [JsonProperty("tsConfigs")]
        public string[] TSConfigFilePaths { get; set; }
    }

    public class AnalysisResponse
    {
        // Other fields omitted
        // i.e. Hightlight[], HighlightedSymbol[], Metrics, CpdToken[]

        [JsonProperty("issues")]
        public IEnumerable<Issue> Issues { get; set; }

        [JsonProperty("parsingError")]
        public ParsingError ParsingError { get; set; }
    }

    public enum ParsingErrorCode
    {
        PARSING,
        MISSING_TYPESCRIPT,
        UNSUPPORTED_TYPESCRIPT,
        FAILING_TYPESCRIPT,
        GENERAL_ERROR
    }

    public class Issue
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }

        [JsonProperty("ruleId")]
        public string RuleId { get; set; }

        [JsonProperty("secondaryLocations")]
        public IssueLocation[] SecondaryLocations { get; set; }

        [JsonProperty("cost")]
        public int? Cost { get; set; }
    }

    public class IssueLocation
    {
        [JsonProperty("line")]
        public int Line { get; set; }

        [JsonProperty("column")]
        public int Column { get; set; }

        [JsonProperty("endLine")]
        public int EndLine { get; set; }

        [JsonProperty("endColumn")]
        public int EndColumn { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; }
    }
}
