﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using SonarJsConfig.Data;

namespace SonarJsConfig
{
    public interface IEslintBridge
    {
        Task Start();
        Task Stop();
        Task<IEnumerable<EslintBridgeIssue>> AnalyzeJS(string filePath, string fileContent);
        Task<IEnumerable<EslintBridgeIssue>> AnalyzeTS(string filePath, string fileContent);
    }

    public sealed class EslintBridgeWrapper : IEslintBridge, IDisposable
    {
        private static class Endpoints
        {
            public const string AnalyzeJs = "analyze-js";
            public const string Close = "close";
        }

        private static readonly IEnumerable<EslintBridgeIssue> EmptyIssues = Array.Empty<EslintBridgeIssue>();

        private const string DownloadUrl = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar";
//        private const string DownloadUrl = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-7.2.0.14938.jar";

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

        public async Task Start()
        {
            await EnsureServerStarted();
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

        public async Task<IEnumerable<EslintBridgeIssue>> AnalyzeJS(string filePath, string fileContent) =>
            await Analyze(filePath, fileContent, EslintRulesProvider.GetJavaScriptRuleKeys());

        public async Task<IEnumerable<EslintBridgeIssue>> AnalyzeTS(string filePath, string fileContent) =>
            await Analyze(filePath, fileContent, EslintRulesProvider.GetTypeScriptRuleKeys());

        private async Task<IEnumerable<EslintBridgeIssue>> Analyze(string filePath, string fileContent, IEnumerable<string> ruleKeys)
        {
            var analysisRequest = CreateRequest(filePath, fileContent, ruleKeys);

            var responseString = await CallNodeServerAsync(Endpoints.AnalyzeJs, analysisRequest);

            if (responseString == null)
            {
                return EmptyIssues;
            }

            var eslintBridgeResponse = JsonConvert.DeserializeObject<EslintBridgeResponse>(responseString);

            if (eslintBridgeResponse.EslintBridgeParsingError != null)
            {
                LogParsingError(filePath, eslintBridgeResponse.EslintBridgeParsingError);
                return EmptyIssues;
            }

            return eslintBridgeResponse.Issues;
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

        private async Task<string> CallNodeServerAsync(string serverEndpoint, string serializedRequest)
        {
            HttpResponseMessage response;
            try
            {
                var timer = Stopwatch.StartNew();

                var requestContent = new StringContent(serializedRequest ?? string.Empty, Encoding.UTF8, "application/json");

                response = await httpClient.PostAsync($"http://localhost:{port}/{serverEndpoint}",
                        requestContent);

                timer.Stop();
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

        private void LogParsingError(string path, EslintBridgeParsingError parsingError)
        {
            //https://github.com/SonarSource/SonarJS/blob/1916267988093cb5eb1d0b3d74bb5db5c0dbedec/sonar-javascript-plugin/src/main/java/org/sonar/plugins/javascript/eslint/AbstractEslintSensor.java#L134
            if (parsingError.ErrorCode == "MISSING_TYPESCRIPT")
            {
                logger.LogMessage("TypeScript dependency was not found and it is required for analysis.");
            }
            else if (parsingError.ErrorCode == "UNSUPPORTED_TYPESCRIPT")
            {
                logger.LogMessage(parsingError.Message);
                logger.LogMessage("If it's not possible to upgrade version of TypeScript used by the project, consider installing supported TypeScript version just for the time of analysis");
            }
            else
            {
                logger.LogMessage($"Failed to parse file [{path}] at line {parsingError.Line}: {parsingError.Message}");
            }
        }

        private string CreateRequest(string filePath, string fileContent, IEnumerable<string> ruleKeys)
        {
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

        public void Dispose()
        {
            ((IDisposable)httpClient).Dispose();
            serverProcess?.Stop();
        }
    }
}
