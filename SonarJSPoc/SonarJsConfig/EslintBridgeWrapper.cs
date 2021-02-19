﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarJsConfig.Config;
using SonarJsConfig.ESLint.Data;

namespace SonarJsConfig
{
    public interface IEslintBridge
    {
        Task<bool> Start();
        Task Stop();
        Task InitLinter(IEnumerable<Rule> rules);
        Task<TSConfigResponse> TSConfigFiles(string configFilePath);
        Task NewTSConfig();
        Task<AnalysisResponse> AnalyzeJS(string filePath, string fileContent, bool ignoreHeaderComments);
        Task<AnalysisResponse> AnalyzeTS(string filePath, string fileContent, bool ignoreHeaderComments);
    }

    public sealed class EslintBridgeWrapper : IEslintBridge, IDisposable
    {
        private static class Endpoints
        {
            public const string TSConfigFiles = "tsconfig-files";
            public const string NewTSConfig = "new-tsconfig";
            public const string InitLinter = "init-linter";
            public const string AnalyzeJs = "analyze-js";
            public const string AnalyzeTs = "analyze-ts";
            public const string Close = "close";
        }

        private static readonly IEnumerable<Issue> EmptyIssues = Array.Empty<Issue>();

        private const string DownloadUrl = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-7.2.0.14938.jar";

        private readonly ILogger logger;
        private readonly SonarJSDownloader downloader;

        private EslintBridgeProcess serverProcess;

        private int port;

        private readonly HttpClient httpClient;

        public EslintBridgeWrapper(ILogger logger)
        {
            this.logger = logger;
            downloader = new SonarJSDownloader();
            httpClient = new HttpClient();
        }

        public async Task<bool> Start()
        {
            return await EnsureServerStarted();
        }

        public async Task Stop()
        {
            if (!serverProcess.IsRunning())
            {
                return;
            }

            await CallNodeServerAsync(Endpoints.Close, null);
            serverProcess.Stop();
        }

        public async Task<TSConfigResponse> TSConfigFiles(string configFilePath)
        {
            var responseString = await CallNodeServerAsync(Endpoints.TSConfigFiles, new TSConfigRequest { TSConfigAbsoluteFilePath = configFilePath });

            var response = JsonConvert.DeserializeObject<TSConfigResponse>(responseString);

            if (response.Error != null)
            {
                logger.LogError($"Parsing error. Code: {response.ErrorCode}, Message: {response.Error}");
            }

            return response;
        }

        public async Task NewTSConfig()
        {
            await CallNodeServerAsync(Endpoints.NewTSConfig, null);
        }

        public async Task InitLinter(IEnumerable<Rule> rules)
        {
            var config = Configuration.CreateFromEnvVars();

            var request = new InitLinterRequest
            {
                Environments = config.getStringArray(GlobalVariableNames.ENVIRONMENTS_PROPERTY_KEY),
                globals = config.getStringArray(GlobalVariableNames.ENVIRONMENTS_PROPERTY_KEY),
                Rules = rules.ToArray()
            };

            await CallNodeServerAsync(Endpoints.InitLinter, request);
        }

        public async Task<AnalysisResponse> AnalyzeJS(string filePath, string fileContent, bool ignoreHeaderComments) =>
            await Analyze(filePath, fileContent, Endpoints.AnalyzeJs, ignoreHeaderComments);

        public async Task<AnalysisResponse> AnalyzeTS(string filePath, string fileContent, bool ignoreHeaderComments) =>
            await Analyze(filePath, fileContent, Endpoints.AnalyzeTs, ignoreHeaderComments);

        private async Task<AnalysisResponse> Analyze(string filePath, string fileContent, string endpoint, bool ignoreHeaderComments)
        {
            var analysisRequest = CreateAnalysisRequest(filePath, fileContent, ignoreHeaderComments);

            var responseString = await CallNodeServerAsync(endpoint, analysisRequest);

            if (responseString == null)
            {
                return null;
            }

            var eslintBridgeResponse = JsonConvert.DeserializeObject<AnalysisResponse>(responseString);

            if (eslintBridgeResponse.ParsingError != null)
            {
                LogParsingError(filePath, eslintBridgeResponse.ParsingError);
            }

            return eslintBridgeResponse;
        }

        private async Task<bool> EnsureServerStarted()
        {
            // Handle the server having stopped unexpectedly
            if (serverProcess != null)
            {
                if (serverProcess.IsRunning())
                {
                    return true;
                }

                logger.LogMessage("Server process has exited. Cleaning up and restarting...");
                serverProcess.Stop();
                serverProcess = null;
            }

            try
            {
                var scriptFilePath = EnsurePluginDownloaded();
                if (scriptFilePath == null)
                {
                    return false;
                }

                serverProcess = new EslintBridgeProcess(logger, scriptFilePath, 0);
                this.port = await serverProcess.StartAsync();
            }
            catch (Exception ex) //when (!ErrorHandler.IsCriticalException(ex))
            {
                await Stop();
                logger.LogError($"Failed to start the server: {ex}");
            }

            return true;
        }

        private string EnsurePluginDownloaded()
        {
            var jarRootDirectory = EnsureSonarJSDownloaded();

            var scriptFilePath = Path.Combine(jarRootDirectory, SonarJSDownloader.EslintBridgeFolderName, "package", "bin", "server");

            if (!File.Exists(scriptFilePath))
            {
                logger.LogError($"eslint-bridge startup script file does not exist: {scriptFilePath}");
                return null;
            }

            return scriptFilePath;
        }

        private string EnsureSonarJSDownloaded() =>
            downloader.Download(DownloadUrl, logger);

        private async Task<string> CallNodeServerAsync(string serverEndpoint, object request)
        {
            var serializedRequest =
                request == null ? "" : JsonConvert.SerializeObject(request, Formatting.Indented);
            var content = new StringContent(serializedRequest ?? string.Empty, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            try
            {
                var timer = Stopwatch.StartNew();
                response = await httpClient.PostAsync($"http://localhost:{port}/{serverEndpoint}", content);

                timer.Stop();
                logger.LogMessage("");
                logger.LogMessage($"Endpoint: {serverEndpoint} Roundtrip: {timer.ElapsedMilliseconds}ms");
            }
            catch (AggregateException ex)
            {
                logger.LogError($"Error connecting to the eslint-bridge server. Please ensure the server is running on port {port}");
                logger.LogError(ex.ToString());
                foreach (var inner in ex.InnerExceptions)
                {
                    logger.LogError("   -------------------------------");
                    logger.LogError(inner.ToString());
                }
                return null;
            }

            var responseString = await response.Content.ReadAsStringAsync();
            logger.LogMessage("Eslint bridge response: " + responseString);

            return responseString;
        }

        private void LogParsingError(string path, ParsingError parsingError)
        {
            //https://github.com/SonarSource/SonarJS/blob/1916267988093cb5eb1d0b3d74bb5db5c0dbedec/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/AbstractEslintSensor.java#L134
            if (parsingError.Code == ParsingErrorCode.MISSING_TYPESCRIPT)
            {
                logger.LogMessage("TypeScript dependency was not found and it is required for analysis.");
            }
            else if (parsingError.Code == ParsingErrorCode.UNSUPPORTED_TYPESCRIPT)
            {
                logger.LogMessage(parsingError.Message);
                logger.LogMessage("If it's not possible to upgrade version of TypeScript used by the project, consider installing supported TypeScript version just for the time of analysis");
            }
            else
            {
                logger.LogMessage($"Failed to parse file [{path}] at line {parsingError.Line}: {parsingError.Code} {parsingError.Message}");
            }
        }

        private AnalysisRequest CreateAnalysisRequest(string filePath, string fileContent, bool ignoreHeaderComments)
        {
            // NOTE: the rule keys we pass to the eslint-bridge are not the Sonar "Sxxxx" keys.
            // Instead, there are more user-friendly keys.
            // We will need to translate between the "Sxxx" and the "friendly" keys.
            // The "friendly" keys are at https://github.com/SonarSource/eslint-plugin-sonarjs/blob/master/src/index.ts
            var eslintRequest = new AnalysisRequest
            {
                FilePath = filePath,
                FileContent = fileContent,
                IgnoreHeaderComments = ignoreHeaderComments
                // TODO TSConfigFilePaths  = ???
            };

            return eslintRequest;
        }

        public void Dispose()
        {
            ((IDisposable)httpClient).Dispose();
            serverProcess?.Stop();
        }
    }
}
