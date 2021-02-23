using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using SonarJsConfig.ESLint.Data;
using SonarJSProto.QualityProfiles;

namespace SonarJsConfig
{
    public class EslintRulesProvider
    {
        public static IEnumerable<string> GetJavaScriptRuleKeys()
            => GetRuleKeysFromResources("SonarJSProto.Resources.js-rules.txt")
                .Except(GetRuleKeysFromResources("SonarJSProto.Resources.ExcludedRules.txt"));

        public static IEnumerable<string> GetTypeScriptRuleKeys()
            => GetRuleKeysFromResources("SonarJSProto.Resources.ts-rules.txt")
                .Except(GetRuleKeysFromResources("SonarJSProto.Resources.ExcludedRules.txt"));

        public static IEnumerable<string> GetJavaScriptVsCodeRuleKeys()
            => GetRuleKeysFromResources("SonarJSProto.Resources.js-rules-vscode.txt");

        public static IEnumerable<string> GetTypeScriptVsCodeRuleKeys()
            => GetRuleKeysFromResources("SonarJSProto.Resources.ts-rules-vscode.txt");

        private static IEnumerable<string> GetRuleKeysFromResources(string resourceName)
        {
            using (var reader = new StreamReader(typeof(EslintRulesProvider).Assembly.GetManifestResourceStream(resourceName)))
            {
                var text = reader.ReadToEnd();
                return text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
        }

        public static readonly IEnumerable<Rule> JavaScript_SonarWay = GetRulesFromQP("SonarCloud_js_sonar_way.xml");
        public static readonly IEnumerable<Rule> JavaScript_SonarWay_Recommended = GetRulesFromQP("SonarCloud_js_sonar_way_recommended.xml");
        public static readonly IEnumerable<Rule> TypeScript_SonarWay = GetRulesFromQP("SonarCloud_ts_sonar_way.xml");
        public static readonly IEnumerable<Rule> TypeScript_SonarWay_Recommended = GetRulesFromQP("SonarCloud_ts_sonar_way_recommended.xml");

        public static IEnumerable<Rule> GetRulesFromQP(string partialResourceName)
            => Convert(QualityProfileLoader.GetProfile(partialResourceName));

        public static IEnumerable<Rule> Convert(profile qualityProfile)
        {
            // TODO - map Sxxx keys to ESLint keys
            return qualityProfile.rules.Select(ToRule).ToArray();
        }

        private static Rule ToRule(rule qualityProfileRule)
        {
            return new Rule
            {
                Key = qualityProfileRule.key,
                Configurations = Array.Empty<string>()
            };
        }
    }
}
