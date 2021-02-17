using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SonarJsConfig
{
    public interface IEslintBridge
    {
        Task Start();
        Task Stop();
    }

    public class EslintBridgeWrapper : IEslintBridge, IDisposable
    {
//        private const string DownloadUrl = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar";
        private const string DownloadUrl = "https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-7.2.0.14938.jar";

        private readonly ILogger logger;
        private readonly SonarJSDownloader downloader;

        private EslintBridgeProcess serverProcess;

        private int port;

        private HttpClient httpClient;

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

            await CallNodeServerAsync("close", null);
            serverProcess.Stop();
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

            var responseString = response.Content.ReadAsStringAsync().Result;
            logger.LogMessage("Eslint bridge response: " + responseString);

            return responseString;
        }

        public void Dispose()
        {
            ((IDisposable)httpClient).Dispose();
            serverProcess?.Stop();
        }
    }
}
