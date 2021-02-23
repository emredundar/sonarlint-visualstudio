using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJSProto.QualityProfiles;

namespace SonarJSPocTests
{
    [TestClass]
    public class QualityProfiles
    {
        [TestMethod]
        [DataRow("SonarCloud_js_sonar_way.xml")]
        [DataRow("SonarCloud_js_sonar_way_recommended.xml")]
        [DataRow("SonarCloud_ts_sonar_way.xml")]
        [DataRow("SonarCloud_js_sonar_way_recommended.xml")]
        public void LoadQPs(string partialResourceName)
        {
            var profile = QualityProfileLoader.GetProfile(partialResourceName);
            profile.Should().NotBeNull();
            profile.rules.Length.Should().BeGreaterThan(0);
        }

    }
}
