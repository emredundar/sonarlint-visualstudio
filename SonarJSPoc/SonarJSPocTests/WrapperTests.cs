using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;
using SonarJsConfig.Config;
using SonarJsConfig.ESLint.Data;

namespace SonarJSPocTests
{
    [TestClass]
    public class WrapperTests
    {
        [TestMethod]
        public async Task Wip()
        {
            using var wrapper = new EslintBridgeWrapper(new ConsoleLogger());

            var started = await wrapper.Start();

            var ruleKeys = EslintRulesProvider.GetTypeScriptRuleKeys();
            var rules = ruleKeys.Select(x => new Rule { Key = x, Configurations = Array.Empty<string>() })
                .ToArray();

            started.Should().BeTrue();
            await wrapper.InitLinter(rules);

            await wrapper.NewTSConfig();

            // Supply the tsconfig file
            var tsConfigFilePath = GetResourceFilePath("Resources", "tsconfig.json");
            var configResposne = await wrapper.TSConfigFiles(tsConfigFilePath);

            // Analyze
            var results = await wrapper.AnalyzeJS("", "//TODO\n", ignoreHeaderComments: false, tsConfigFilePath);

            results.Issues.Count().Should().Be(1);
        }

        [TestMethod]
        public async Task TsConfigMapping()
        {
            const string baseDirectory = @"D:\repos\sq\other\SonarJS\eslint-bridge";

            var logger = new ConsoleLogger();
            using var wrapper = new EslintBridgeWrapper(logger);

            var started = await wrapper.Start();

            var testSubject = new TsConfigMapper(wrapper, logger);

            await testSubject.InitializeAsync(baseDirectory);
        }

        private string GetResourceFilePath(params string[] parts)
        {
            var allParts = new List<string>();
            allParts.Add(Path.GetDirectoryName(typeof(WrapperTests).Assembly.Location));

            allParts.AddRange(parts);

            var fullPath = Path.Combine(allParts.ToArray());
            fullPath = fullPath.Replace("\\", "/");

            File.Exists(fullPath).Should().BeTrue();
            return fullPath;
        }
    }
}
