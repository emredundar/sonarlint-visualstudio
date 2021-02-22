using Newtonsoft.Json;

namespace SonarJsConfig.ESLint.Data
{

    // "init-linter" needs to be called before running an analysis.
    // Three fields: rules, environments and globals.

    // See
    // Official docs: https://docs.sonarqube.org/pages/viewpage.action?pageId=1441944

    // sonar.javascript.environments:
    // Comma-separated list of environments names.The analyzer automatically adds global variables based on that list.
    // Available environment names: amd, applescript, atomtest, browser, commonjs, couch, embertest, greasemonkey, jasmine, jest, jquery, meteor, mocha, mongo, nashorn, node, phantomjs, prototypejs, protractor, qunit, rhino, serviceworker, shared-node-browser, shelljs, webextensions, worker, wsh, yui.
    // By default all environments are included.

    // sonar.javascript.globals
    // Comma-separated list of global variables.
    // Default value is "angular,goog,google,OpenLayers,d3,dojo,dojox,dijit,Backbone,moment,casper".

    // JS plugin code: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/javascript-frontend/src/main/java/org/sonar/javascript/tree/symbols/GlobalVariableNames.java#L51

    public class InitLinterRequest
    {
        // Java version: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/EslintBridgeServerImpl.java

        [JsonProperty("rules")]
        public Rule[] Rules { get; set; }

        [JsonProperty("environments")]
        public string[] Environments { get; set; }

        [JsonProperty("globals")]
        public string[] globals { get; set; }
    }

    public class Rule
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("configurations")]
        public string[] Configurations { get; set; }
    }
}
