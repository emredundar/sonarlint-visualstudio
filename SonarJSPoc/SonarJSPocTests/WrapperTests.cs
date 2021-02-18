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

            var results = await wrapper.AnalyzeJS("", "//TODO\n");

            results.Count().Should().Be(1);
        }
    }
}
