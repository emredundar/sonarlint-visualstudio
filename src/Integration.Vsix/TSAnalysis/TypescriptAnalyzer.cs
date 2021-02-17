using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio;
using Newtonsoft.Json;
using SonarJsConfig;
using SonarJsConfig.Data;
using Sonarlint;
using SonarLint.VisualStudio.Integration.Vsix.Analysis;

namespace SonarLint.VisualStudio.Integration.Vsix.TSAnalysis
{

    // TODO:
    // * launch server and capture port
    //      * assume we want launch the server, capture the output streams, and leave it running in the background.
    // * check how parsing errors are handled (?don't want to display them to the user?)
    //      * understand set of possible server error codes: e.g. missing TS, missing node, wrong version of node
    // * experiment with passing "fileContent" instead/as well as "path"


    // * investigate embedding artefacts in the VSIX: likely size increase if we embed e.g. the minimum required bits, TypeScript, node
    // * discover list of available rule keys: from files in the jar? API calls at build time?
    // * location of node in VS / location/existence of TS?
    // * number of Java-based vs Node-based rules

    [Export(typeof(IAnalyzer))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public partial class TypescriptAnalyzer : IAnalyzer
    {
        private readonly ILogger logger;
        private int port;
        private readonly string serverStartupScriptLocation;

        private EslintBridgeServerStarter serverStarter;

        [ImportingConstructor]
        public TypescriptAnalyzer(ILogger logger) 
            :this(logger, 0, null)
        {
        }

        internal TypescriptAnalyzer(ILogger logger, int port, string serverStartupScriptLocation)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.port = port;
            this.serverStartupScriptLocation = serverStartupScriptLocation;
    }

        public bool IsAnalysisSupported(IEnumerable<AnalysisLanguage> languages)
        {
            if (languages == null)
            {
                throw new ArgumentNullException(nameof(languages));
            }

            return languages.Contains(AnalysisLanguage.Javascript) ||
                languages.Contains(AnalysisLanguage.Typescript);
        }

        public void ExecuteAnalysis(string path, string charset, IEnumerable<AnalysisLanguage> detectedLanguages,
            IIssueConsumer consumer,
            ProjectItem projectItem)
        {
            if (!IsAnalysisSupported(detectedLanguages))
            {
                throw new ArgumentOutOfRangeException($"Unsupported language");
            }

            if (!EnsureServerStarted())
            {
                return;
            }

            var language = detectedLanguages.Contains(AnalysisLanguage.Typescript)
                ? AnalysisLanguage.Typescript : AnalysisLanguage.Javascript;

            var fileContent = string.Empty; //GetFileContent(projectItem);


            var serializedRequest = this.CreateRequest(path, fileContent, language);

            // If the server can't find typescript then response will contain "MISSING_TYPESCRIPT".
            // To fix this, either:
            // 1) add the "typescript" under the "node_modules" folder of the eslint-bridge server, or
            // 2) set the environment variable NODE_PATH to the a "node_modules" folder that contains "typescript".
            //      NB the variable must be set before launching the server.
            var serverEndpoint = language == AnalysisLanguage.Typescript ?
                "analyze-ts" : "analyze-js";

            var responseString = CallEslintBridge(serverEndpoint, serializedRequest);

            if (responseString == null)
            {
                return;
            }

            var timer = Stopwatch.StartNew();

            var eslintBridgeResponse = JsonConvert.DeserializeObject<EslintBridgeResponse>(responseString);

            if (eslintBridgeResponse.EslintBridgeParsingError != null)
            {
                LogParsingError(path, eslintBridgeResponse.EslintBridgeParsingError);
                return;
            }

            var analysisIssues = eslintBridgeResponse.Issues.Select(x =>
                new Issue
                {
                    EndLine = x.EndLine ?? 0,
                    Message = x.Message,
                    RuleKey = "javascript:" + x.RuleId,
                    StartLine = x.Line,
                    FilePath = path
                });

            consumer.Accept(path, analysisIssues);
            timer.Stop();

            logger.WriteLine($"Number of issues returned: {analysisIssues.Count()}");
            logger.WriteLine($"Time for consumer to process issues: {timer.ElapsedMilliseconds}ms");
        }

        private string CallEslintBridge(string serverEndpoint, string serializedRequest)
        {
            HttpResponseMessage response = null;
            try
            {
                using var httpClient = new System.Net.Http.HttpClient();

                var timer = Stopwatch.StartNew();

                response = httpClient.PostAsync($"http://localhost:{port}/{serverEndpoint}",
                        new StringContent(serializedRequest, Encoding.UTF8, "application/json"))
                    .Result;

                timer.Stop();
                logger.WriteLine($"VS->eslint-bridge roundtrip: {timer.ElapsedMilliseconds}ms");
                
            }
            catch (AggregateException ex)
            {
                logger.WriteLine($"Error connecting to the eslint-bridge server. Please ensure the server is running on port {port}");
                logger.WriteLine(ex.ToString());
                foreach (var inner in ex.InnerExceptions)
                {
                    logger.WriteLine("   -------------------------------");
                    logger.WriteLine(inner.ToString());
                }
                return null;
            }

            var responseString = response.Content.ReadAsStringAsync().Result;
            logger.WriteLine("Eslint bridge response: " + responseString);

            return responseString;
        }


        private bool EnsureServerStarted()
        {
            // Handle the server having stopped unexpectedly
            if (serverStarter != null)
            {
                if (serverStarter.IsRunning())
                {
                    return true;
                }

                logger.WriteLine("Server process has exited. Cleaning up and restarting...");
                serverStarter.Dispose();
                serverStarter = null;
            }

            try
            {
                var scriptFilePath = EnsurePluginDownloaded();
                if (scriptFilePath == null)
                {
                    return false;
                }

                serverStarter = new EslintBridgeServerStarter(logger, scriptFilePath, port);
                this.port = serverStarter.Start().Result;
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                serverStarter.Dispose();
                logger.WriteLine($"ERROR: Failed to start the server: {ex}");
            }

            return true;
        }

        private string EnsurePluginDownloaded()
        {
            var jarRootDirectory = EnsureSonarJSDownloaded();

            var scriptFilePath = serverStartupScriptLocation ??
                System.IO.Path.Combine(jarRootDirectory, SonarJsConfig.SonarJSDownloader.EslintBridgeFolderName, "package", "bin", "server");

            if (!File.Exists(scriptFilePath))
            {
                logger.WriteLine($"ERROR: eslint-bridge startup script file does not exist: {scriptFilePath}");
                return null;
            }

            return scriptFilePath;
        }

        private string GetFileContent(ProjectItem projectItem)
        {
             var fileContent = "";
            if (projectItem != null)
            {
                var textDocument = (projectItem.Document.Object() as TextDocument);
                var editPoint = textDocument.StartPoint.CreateEditPoint();
                fileContent = editPoint.GetText(textDocument.EndPoint);
            }
            return fileContent;
        }

        private string CreateRequest(string filePath, string fileContent, AnalysisLanguage language)
        {
            var ruleKeys = language == AnalysisLanguage.Javascript
                ? EslintRulesProvider.GetJavaScriptRuleKeys()
                : EslintRulesProvider.GetTypeScriptRuleKeys();

            // NOTE: the rule keys we pass to the eslint-bridge are not the Sonar "Sxxxx" keys.
            // Instead, there are more user-friendly keys.
            // We will need to translate between the "Sxxx" and the "friendly" keys.
            // The "friendly" keys are at https://github.com/SonarSource/eslint-plugin-sonarjs/blob/master/src/index.ts
            var eslintRequest = new EslintBridgeRequest
            {
                FilePath = filePath,
                FileContent = fileContent,
                Rules = ruleKeys.Select(x => new EsLintRuleConfig { Key = x, Configurations = Array.Empty<string>() })
                    .ToArray()
            };

            var serializedRequest = JsonConvert.SerializeObject(eslintRequest, Formatting.Indented);
            return serializedRequest;
        }

        private void LogParsingError(string path, EslintBridgeParsingError parsingError)
        {
            //https://github.com/SonarSource/SonarJS/blob/1916267988093cb5eb1d0b3d74bb5db5c0dbedec/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/AbstractEslintSensor.java#L134
            if (parsingError.ErrorCode == "MISSING_TYPESCRIPT")
            {
                logger.WriteLine("TypeScript dependency was not found and it is required for analysis.");
            } 
            else if (parsingError.ErrorCode == "UNSUPPORTED_TYPESCRIPT")
            {
                logger.WriteLine(parsingError.Message);
                logger.WriteLine("If it's not possible to upgrade version of TypeScript used by the project, consider installing supported TypeScript version just for the time of analysis");
            }
            else
            {
                logger.WriteLine($"Failed to parse file [{path}] at line {parsingError.Line}: {parsingError.Message}");
            }
        }

        private string EnsureSonarJSDownloaded()
        {

            var downloader = new SonarJsConfig.SonarJSDownloader();
            var url = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar";
            var outputDir = downloader.Download(url, new LoggerAdapter(logger));

            return outputDir;
        }

        private class LoggerAdapter : SonarJsConfig.ILogger
        {
            private readonly ILogger vsLogger;

            public LoggerAdapter(ILogger vsLogger)
            {
                this.vsLogger = vsLogger;
            }

            void SonarJsConfig.ILogger.LogError(string message)
            {
                vsLogger.WriteLine("ERROR: " + message);
            }

            void SonarJsConfig.ILogger.LogMessage(string message)
            {
                vsLogger.WriteLine(message);
            }
        }
    }
}
