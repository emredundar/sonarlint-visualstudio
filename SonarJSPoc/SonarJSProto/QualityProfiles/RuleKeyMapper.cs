using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using SonarJsConfig;
using SonarLint.VisualStudio.Integration;

namespace SonarJSProto.QualityProfiles
{
    /* Notes:
     * 
     * SonarSource rules have two keys: the Sxxx version used in the Quality Profile etc, and a different key
     * used by ESLint. External ESLint rules don't have an Sxxx key.
     * 
     * We need to translate between the SQ and ESLint keys:
     * - when fetching the rule config from the server, we'll get the Sxxx version of the key, but we need to pass the
     *   ESLint version to the analyzer.
     * 
     * 
     * There isn't currently a config/settings file we can read the maps between the ESLint and Sxxx keys.
     * This class is a hacky workaround for prototyping purposes. In the plugin, there is generated file for
     * each rule under eslint-bridge\package\lib\rules (there is tooling to generate the scaffolding classes for a new rule).
     * The name of the generated file will be the ESLint name, and for most* Sonar rules they will have a 
     * generated link to the RSPEC e.g. https://jira.sonarsource.com/browse/RSPEC-1234
     * 
     * This class parses the files on disc and returns a mapping.
     * 
     * Note: this is an incomplete mapping. Some files don't have an Sxxx key. Also, some Sonar rules that 
     * appear in the recommended QP don't have .js file.
     * 
     */


    /// <summary>
    /// Hacky class to map between SonarSource rule keys and ESLint rule keys.
    /// </summary>
    public class RuleKeyMapper
    {
        private const string RspecPattern = "^// https://jira.sonarsource.com/browse/RSPEC-(.+)$";
        private static readonly Regex RspecRegEx = new Regex(RspecPattern, RegexOptions.Compiled | RegexOptions.Multiline);

        private IDictionary<string, string> sonarToEsLintKey;
        private IDictionary<string, string> esLintToSonarKey;

        private readonly ILogger logger;

        public static RuleKeyMapper GenerateMappingFromFiles(string rootRulesDirectory, ILogger logger)
        {
            var mapper = new RuleKeyMapper(logger);
            mapper.Initialize(rootRulesDirectory);
            return mapper;
        }

        public IReadOnlyDictionary<string, string> SonarToEsLintKey => (IReadOnlyDictionary<string, string>)sonarToEsLintKey;
        public IReadOnlyDictionary<string, string> EsLintToSonarKey => (IReadOnlyDictionary<string, string>)esLintToSonarKey;

        public string ToSonarKey(string esLintKey)
        {
            if(esLintToSonarKey.TryGetValue(esLintKey, out var sonar))
            {
                return sonar;
            }
            // Otherwise, assume it's an external rule key and use that for the Sonar key
            return esLintKey;
        }

        public string ToEsLintKey(string sonarKey)
        {
            if(sonarToEsLintKey.TryGetValue(sonarKey, out var eslint))
            {
                return eslint;
            }

            // ???
            Debug.WriteLine($"[ESLint Rule Mapper] Failed to map from sonarKey to eslint key: {sonarKey}");

            return null;
        }

        private RuleKeyMapper(ILogger logger)
        {
            this.logger = logger;
            sonarToEsLintKey = new Dictionary<string, string>();
            esLintToSonarKey = new Dictionary<string, string>();
        }

        private void Initialize(string rootRulesDirectory)
        {
            var files = GetRulesFiles(rootRulesDirectory);
            logger.LogMessage($"[ESLint Rule Mapper] : found {files.Count()} rules files");

            foreach (var file in files)
            {
                ProcessFile(file);
            }
        }

        private static IEnumerable<string> GetRulesFiles(string rootRulesDirectory) =>
            Directory.GetFiles(rootRulesDirectory, "*.js", SearchOption.TopDirectoryOnly)
                .ToArray();

        private void ProcessFile(string fileName)
        {
            var text = File.ReadAllText(fileName);
            var match = RspecRegEx.Match(text);

            if (match.Success)
            {
                var key = match.Groups[1].Value;
                Debug.Assert(int.TryParse(key, out var _), $"Expected the rule identifier in the file to be numeric. Actual: {key}");

                var sonarKey = "S" + key;
                var eslintKey = Path.GetFileNameWithoutExtension(fileName);

                try
                {
                    sonarToEsLintKey.Add(sonarKey, eslintKey);
                    esLintToSonarKey.Add(eslintKey, sonarKey);
                }
                catch (Exception ex)
                {
                    logger.LogError($"[ESLint Rule Mapper] : error processing file {fileName}: {ex.Message}");
                }
            }
            else
            {
                logger.LogMessage($"[ESLint Rule Mapper] : failed to process file {fileName}");
            }
        }
    }
}
