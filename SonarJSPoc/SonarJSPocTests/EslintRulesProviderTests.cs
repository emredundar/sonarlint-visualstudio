using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
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
        public async Task GetRulesFromQualityProfiles()
        {
            using var wrapper = new EslintBridgeWrapper(new ConsoleLogger());

            var started = await wrapper.Start();

            var testSubject = new EslintRulesProvider(wrapper.RuleKeyMapper);

            var rules = testSubject.JavaScript_SonarWay;
            rules = testSubject.JavaScript_SonarWay_Recommended;
            rules = testSubject.TypeScript_SonarWay;
            rules = testSubject.TypeScript_SonarWay_Recommended;

            await wrapper.Stop();
        }


        [TestMethod]
        public async Task RuleSerialization()
        {
            using var wrapper = new EslintBridgeWrapper(new ConsoleLogger());

            var started = await wrapper.Start();

            var testSubject = new EslintRulesProvider(wrapper.RuleKeyMapper);

            var rules = testSubject.TypeScript_SonarWay_Recommended;

            var json = JsonConvert.SerializeObject(rules, Formatting.Indented);

            Console.WriteLine(json);

            await wrapper.Stop();
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
