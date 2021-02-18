using System.Linq;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SonarJsConfig.Config;

namespace SonarJSPocTests
{
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void CreateFromEnvVars_Environments_NoEnvVars_UsesDefaults()
        {
            var expected = ("amd, applescript, atomtest, browser, commonjs, couch, embertest, flow, greasemonkey, jasmine, jest, jquery, " +
          "meteor, mocha, mongo, nashorn, node, phantomjs, prototypejs, protractor, qunit, rhino, serviceworker, shared-node-browser, shelljs, webextensions, worker, wsh, yui")
                .Split(new string[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            var testSubject = Configuration.CreateFromEnvVars();

            var actual = testSubject.getStringArray(GlobalVariableNames.ENVIRONMENTS_PROPERTY_KEY);

            actual.Should().BeEquivalentTo(expected);
        }

        [TestMethod]
        public void CreateFromEnvVars_Globals_NoEnvVars_UsesDefaults()
        {
            var expected = "angular,goog,google,OpenLayers,d3,dojo,dojox,dijit,Backbone,moment,casper"
                .Split(new string[] { "," }, System.StringSplitOptions.RemoveEmptyEntries)
                .ToArray();

            var testSubject = Configuration.CreateFromEnvVars();

            var actual = testSubject.getStringArray(GlobalVariableNames.GLOBALS_PROPERTY_KEY);

            actual.Should().BeEquivalentTo(expected);

        }


    }

}
