using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using Newtonsoft.Json;

namespace SonarJsConfig.Config
{
    // Ported from Java class: https://github.com/SonarSource/SonarJS/blob/0dda9105bab520569708e230f4d2dffdca3cec74/javascript-frontend/src/main/java/org/sonar/javascript/tree/symbols/GlobalVariableNames.java
    public static class GlobalVariableNames
    {
        public const string ENVIRONMENTS_PROPERTY_KEY = "sonar.javascript.environments";
        public const string ENVIRONMENTS_DEFAULT_VALUE = "amd, applescript, atomtest, browser, commonjs, couch, embertest, flow, greasemonkey, jasmine, jest, jquery, " +
          "meteor, mocha, mongo, nashorn, node, phantomjs, prototypejs, protractor, qunit, rhino, serviceworker, shared-node-browser, shelljs, webextensions, worker, wsh, yui";

        public const string GLOBALS_PROPERTY_KEY = "sonar.javascript.globals";
        public const string GLOBALS_DEFAULT_VALUE = "angular,goog,google,OpenLayers,d3,dojo,dojox,dijit,Backbone,moment,casper";

        // ********************************************************************************
        // NOTE:
        // The code below is a port of the corresponding Java class.
        //
        //          IT PROBABLY ISN'T NEEDED BY SLVS
        // 
        // All it does is build a unique list called "names" of all the global names in the
        // current set of valid enviroments.
        //
        // However, it looks like this is only used by the legacy javascript-frontend in
        // HoistedSymbolVisitor.addExternalSymbols i.e. so the legacy parser knows which
        // names to treat as symbols.
        // ********************************************************************************

        // Map of recognised enviroments to global variables in those environements
        internal /* for testing */ static IDictionary<string, ISet<string>> ENVIRONMENTS = environments();

        /// <summary>
        /// List of global names to use during analysis
        /// </summary>
        private static ISet<string> names;

        public static void Initialize(IConfiguration configuration)
        {
            // Builds the list of "names" to return.

            // The globals.json file contains a map of the allowed "environment" -> global names.
            var namesBuilder = ImmutableArray.CreateBuilder<string>();

            // The values in the "builtin" environment are always used.
            namesBuilder.AddRange(globalsFromEnvironment("builtin"));

            if (configuration != null)
            {
                // Add whatever is in "sonar.javascript.globals"
                namesBuilder.AddRange(configuration.getStringArray(GLOBALS_PROPERTY_KEY));

                // Now loop through the environments specified in "sonar.javascript.environments".
                // If the enviroment is recognised (i.e. if they are in globals.json) then add its contentss
                foreach(var environmentName in configuration.getStringArray(ENVIRONMENTS_PROPERTY_KEY))
                {
                    var namesFromCurrentEnvironment = globalsFromEnvironment(environmentName);

                    if (namesFromCurrentEnvironment != null)
                    {
                        namesBuilder.AddRange(namesFromCurrentEnvironment);
                    }
                    else
                    {
                        Debug.Write($"{nameof(ENVIRONMENTS_PROPERTY_KEY)} contains an unknown environment: {environmentName}");
                    }
                }
            }

            names = namesBuilder.ToImmutableHashSet();
        }

        /// <summary>
        /// Load the data from the globals.json file into a map of environment name -> globals in that environment
        /// </summary>
        internal /* for testing */ static Dictionary<string, ISet<string>> environments()
        {
            var resourceName = "SonarJsConfig.Resources.globals.json";

            string text;
            using (var reader = new StreamReader(typeof(EslintRulesProvider).Assembly.GetManifestResourceStream(resourceName)))
            {
                text = reader.ReadToEnd();
            }

            var map = JsonConvert.DeserializeObject<Dictionary<string, ISet<string>>>(text);
            return map;
        }

        public static ISet<string> Names => names;

        private static IEnumerable<string> globalsFromEnvironment(string environment)
        {
            if (ENVIRONMENTS.TryGetValue(environment, out var value))
            {
                return value;
            }
            return null;
        }
    }

}
