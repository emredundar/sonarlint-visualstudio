using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;

namespace SonarJSPocTests
{
    [TestClass]
    public class EslintRulesProviderTests
    {
        public TestContext TestContext { get; set; }

        [TestMethod]
        public void GetJavascriptRules()
        {
            var actual = EslintRulesProvider.GetJavaScriptRuleKeys();

            actual.Should().NotBeEmpty();

            DumpKeys(actual);
        }

        [TestMethod]
        public void GetTypeScriptRules()
        {
            var actual = EslintRulesProvider.GetTypeScriptRuleKeys();

            actual.Should().NotBeEmpty();

            DumpKeys(actual);
        }

        [TestMethod]
        public void GetRulesFromQualityProfiles()
        {
            var rules = EslintRulesProvider.JavaScript_SonarWay;
            rules = EslintRulesProvider.JavaScript_SonarWay_Recommended;
            rules = EslintRulesProvider.TypeScript_SonarWay;
            rules = EslintRulesProvider.TypeScript_SonarWay_Recommended;
        }

        private void DumpKeys(IEnumerable<string> ruleKeys)
        {
            foreach (var item in ruleKeys)
            {
                TestContext.WriteLine(item);
            }
        }
    }
}
