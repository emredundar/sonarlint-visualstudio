namespace SonarJsConfig.Data
{
    public static class GlobalVariableNames
    {
        // Java class: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/javascript-frontend/src/main/java/org/sonar/javascript/tree/symbols/GlobalVariableNames.java

        public const string ENVIRONMENTS_PROPERTY_KEY = "sonar.javascript.environments";
        public const string ENVIRONMENTS_DEFAULT_VALUE = "amd, applescript, atomtest, browser, commonjs, couch, embertest, flow, greasemonkey, jasmine, jest, jquery, " +
          "meteor, mocha, mongo, nashorn, node, phantomjs, prototypejs, protractor, qunit, rhino, serviceworker, shared-node-browser, shelljs, webextensions, worker, wsh, yui";

        public const string GLOBALS_PROPERTY_KEY = "sonar.javascript.globals";
        public const string GLOBALS_DEFAULT_VALUE = "angular,goog,google,OpenLayers,d3,dojo,dojox,dijit,Backbone,moment,casper";


        public static readonly string[] DefaultEnvironments = ENVIRONMENTS_DEFAULT_VALUE.Split(new string[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);
        public static readonly string[] DefaultGlobals = GLOBALS_DEFAULT_VALUE.Split(new string[] { ", " }, System.StringSplitOptions.RemoveEmptyEntries);

    }
}
