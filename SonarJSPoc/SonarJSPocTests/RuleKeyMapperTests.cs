using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJSProto.QualityProfiles;

namespace SonarJSPocTests
{
    [TestClass]
    public class RuleKeyMapperTests
    {
        [TestMethod]
        public void CreateMapping()
        {
            var rootDir = @"C:\Users\jdcp\AppData\Local\SLVS_Internal_Build\SonarJS\sonar-javascript-plugin-7.2.0.14938\eslint-bridge\package\lib\rules";

            var result = RuleKeyMapper.GenerateMappingFromFiles(rootDir, new ConsoleLogger());

            result.SonarToEsLintKey.Count.Should().BeGreaterThan(100);
            result.SonarToEsLintKey.Count.Should().Be(result.EsLintToSonarKey.Count);
        }
    }
}
