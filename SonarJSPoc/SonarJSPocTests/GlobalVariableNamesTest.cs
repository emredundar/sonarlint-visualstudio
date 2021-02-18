using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig.Config;

namespace SonarJSPocTests
{
    [TestClass]
    public class GlobalVariableNamesTest
    {
        [TestMethod]
        public void Names_LoadsCorrectlyFromResources()
        {
            var envs = GlobalVariableNames.ENVIRONMENTS;

            envs.Count.Should().Be(30);
        }
    }
}
