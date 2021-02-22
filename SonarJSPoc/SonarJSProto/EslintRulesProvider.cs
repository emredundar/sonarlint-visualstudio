using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

        private static IEnumerable<string> GetRuleKeysFromResources(string resourceName)
        {
            using (var reader = new StreamReader(typeof(EslintRulesProvider).Assembly.GetManifestResourceStream(resourceName)))
            {
                var text = reader.ReadToEnd();
                return text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            }
        }
    }
}
