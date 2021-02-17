using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SonarJsConfig
{
    /// <summary>
    /// Wraps the low-level .NET Process object used to launch and terminate the NodeJS process
    /// </summary>
    public sealed class EslintBridgeProcess
    {
        private readonly ILogger logger;
        private readonly string serverStartupScriptLocation;
        private int port;

        private TaskCompletionSource<int> startTask;
        private Process process;
        private int processId;

        public EslintBridgeProcess(ILogger logger, string serverStartupScriptLocation, int port)
        {
            this.logger = logger;
            this.serverStartupScriptLocation = serverStartupScriptLocation;
            this.port = port;
        }

        public int ProcessId => processId;

        public Task<int> StartAsync()
        {
            if (startTask == null)
            {
                startTask = new TaskCompletionSource<int>();
                StartServer();
            }

            return startTask.Task;
        }

        public bool IsRunning()
            => !this.process?.HasExited ?? false;

        private void StartServer()
        {
            var nodePath = "node.exe ";
            var command = $"{serverStartupScriptLocation} {port}";

            var psi = new ProcessStartInfo
            {
                FileName = nodePath,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false, // required if we want to capture the error output
                ErrorDialog = false,
                CreateNoWindow = true,
                Arguments = command
            };

            psi.EnvironmentVariables.Add("NODE_PATH", @"%APPDATA%\npm\node_modules");

            process = new Process { StartInfo = psi };
            process.ErrorDataReceived += OnErrorDataReceived;
            process.OutputDataReceived += OnOutputDataReceived;

            process.Start();
            processId = process.Id;
            logger.LogMessage($"ESLINT-BRIDGE: Server process id: {process.Id}");
            logger.LogMessage($"ESLINT-BRIDGE: Server process HasExited: {process.HasExited}");

            process.BeginErrorReadLine();
            process.BeginOutputReadLine();
        }

        private void OnOutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null)
            {
                return;
            }

            logger.LogMessage("ESLINT-BRIDGE: " + e.Data);

            var portMessage = Regex.Matches(e.Data, @"port\s+(\d+)");

            if (portMessage.Count > 0)
            {
                var portNumber = int.Parse(portMessage[0].Groups[1].Value);

                if (portNumber != 0)
                {
                    port = portNumber;
                    startTask.SetResult(portNumber);
                }
            }
        }

        private void OnErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            logger.LogError("ESLINT-BRIDGE: " + e.Data);
        }

        public void Stop()
        {
            if (process.HasExited)
            {
                logger.LogMessage("Node process has already terminated");
                return;
            }

            process.Kill();
            process?.Dispose();
            logger.LogMessage("Node process killed");
            process = null;
        }
    }
}
