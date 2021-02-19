using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig;

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

            started.Should().BeTrue();
            await wrapper.InitLinter();

            await wrapper.NewTSConfig();

            // Supply the tsconfig file
            var tsConfigFilePath = GetResourceFilePath("Resources", "tsconfig.json");
            await wrapper.TSConfigFiles(tsConfigFilePath);

            // Analyze
            var results = await wrapper.AnalyzeJS("", "//TODO\n");

            results.Count().Should().Be(1);
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
