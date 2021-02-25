using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

        public EslintRulesProvider(RuleKeyMapper ruleKeyMapper)
        {
            JavaScript_SonarWay = GetRulesFromQP("SonarCloud_js_sonar_way.xml", ruleKeyMapper);
            JavaScript_SonarWay_Recommended = GetRulesFromQP("SonarCloud_js_sonar_way_recommended.xml", ruleKeyMapper);
            TypeScript_SonarWay = GetRulesFromQP("SonarCloud_ts_sonar_way.xml", ruleKeyMapper);
            TypeScript_SonarWay_Recommended = GetRulesFromQP("SonarCloud_ts_sonar_way_recommended.xml", ruleKeyMapper);
        }

        public IEnumerable<Rule> JavaScript_SonarWay { get; }
        public IEnumerable<Rule> JavaScript_SonarWay_Recommended { get; }
        public IEnumerable<Rule> TypeScript_SonarWay { get; }
        public IEnumerable<Rule> TypeScript_SonarWay_Recommended { get; }

        public static IEnumerable<Rule> GetRulesFromQP(string partialResourceName, RuleKeyMapper ruleKeyMapper)
            => Convert(QualityProfileLoader.GetProfile(partialResourceName), ruleKeyMapper);

        private static IEnumerable<Rule> Convert(profile qualityProfile, RuleKeyMapper ruleKeyMapper)
        {
            return qualityProfile.rules
                .Select(x => ToRule(x, ruleKeyMapper))
                .Where(x => x.Key != null)
                .ToArray();
        }

        private static Rule ToRule(rule qualityProfileRule, RuleKeyMapper ruleKeyMapper)
        {
            var esLintKey = ruleKeyMapper.ToEsLintKey(qualityProfileRule.key);
            object[] configurations = Array.Empty<string>();
            if ((qualityProfileRule.parameters?.Length ?? 0) > 0)
            {
                configurations = qualityProfileRule.parameters.Select(ToJObject).ToArray();

                var paramsAsText = string.Join(", ", qualityProfileRule.parameters.Select(x => $"{x.key}={x.value}"));
                System.Diagnostics.Debug.WriteLine($"Parameterised rule: [{qualityProfileRule.key}, {esLintKey}] Params: {paramsAsText}");
            }

            return new Rule
            {
                Key = ruleKeyMapper.ToEsLintKey(qualityProfileRule.key) ,
                Configurations = configurations
            };
        }

        private static JObject ToJObject(parameter ruleParam)
        {
            // TODO - check format is correctly (don't think it is)
            var jObject = new JObject();
            jObject.Add(ruleParam.key, new JValue(ruleParam.value));
            return jObject;
        }

    }
}
