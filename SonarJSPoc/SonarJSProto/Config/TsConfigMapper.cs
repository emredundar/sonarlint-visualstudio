using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.Integration.Helpers;

namespace SonarJsConfig.Config
{
    /// <summary>
    /// Maintains a stateful mapping between tsconfig.json files and the
    /// source files to which they apply
    /// </summary>
    public interface ITsConfigMapper
    {
        Task InitializeAsync(string baseDirectory);

        /// <summary>
        /// Returns the path to the appropriate tsconfig.json file for the specified
        /// source file, or null if one could not be found
        /// </summary>
        /// <returns></returns>
        string FindTsConfigFile(string sourceFilePath);

        /// <summary>
        /// Clear any cached information
        /// </summary>
        void Reset();
    }

    [Export(typeof(ITsConfigMapper))]
    [PartCreationPolicy(CreationPolicy.Shared)]
    public class TsConfigMapper : ITsConfigMapper
    {
        private readonly IEslintBridge eslintBridge;
        private readonly ILogger logger;

        private IEnumerable<TsConfigFile> configFiles;

        [ImportingConstructor]
        public TsConfigMapper(IEslintBridge eslintBridge, ILogger logger)
        {
            this.eslintBridge = eslintBridge;
            this.logger = logger;
        }

        public async Task InitializeAsync(string baseDirectory)
        {
            var locator = new TsConfigLocator();
            var files = locator.Locate(baseDirectory);

            configFiles = await ProcessTsConfigFiles(files);
        }

        public string FindTsConfigFile(string sourceFilePath)
        {
            // TODO: we might want to change the mapping so we're directly mapping 
            // from the source file to the applicable config file

            return configFiles?.FirstOrDefault(config => config.Files.Contains(sourceFilePath))
                ?.FileName;
        }

        public void Reset()
        {
            configFiles = new List<TsConfigFile>();
        }

        // Logically does the same as TypeScriptSensor.java::loadTsConfigs(List<string> tsConfigPaths)
        // i.e. builds a list of tsconfig files mapped to the individual source files to which they apply
        private async Task<IEnumerable<TsConfigFile>> ProcessTsConfigFiles(IEnumerable<string> filePaths)
        {
            var tsConfigFiles = new List<TsConfigFile>();

            var serverStarted = await eslintBridge.Start();
            if (!serverStarted)
            {
                logger.LogMessage("ESLintBridge server did not start");
                return tsConfigFiles;
            }

            Queue<string> worklist = new Queue<string>(filePaths);
            var processed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            while(worklist.Count > 0)
            {
                var path = worklist.Dequeue();
                if (processed.Contains(path))
                {
                    continue;
                }

                var tsConfig = await ParseTsConfigFileAsync(path);
                tsConfigFiles.Add(tsConfig);

                if (tsConfig.ProjectReferences.Any())
                {
                    logger.LogDebug($"Adding referenced project's tsconfigs: { string.Join(Environment.NewLine, tsConfig.ProjectReferences)}");
                    foreach (var projectRef in tsConfig.ProjectReferences)
                    {
                        worklist.Enqueue(projectRef);
                    }

                }

                processed.Add(path);
            }

            return tsConfigFiles;
        }

        // EslintBridgeServerImpl.java::loadTsConfig(string fileName)
        private async Task<TsConfigFile> ParseTsConfigFileAsync(string filePath)
        {
            var response = await eslintBridge.TSConfigFiles(filePath);
            if (response.Error != null)
            {
                logger.LogError(response.Error);
            }

            return new TsConfigFile(GetCanonicalPath(filePath),
                response.Files.Select(GetCanonicalPath),
                response.ProjectReferences.Select(GetCanonicalPath));
        }

        private static string GetCanonicalPath(string path) => System.IO.Path.GetFullPath(path);

        private class TsConfigFile
        {
            public TsConfigFile(string fileName, IEnumerable<string> files, IEnumerable<string> projectReferences)
            {
                FileName = fileName;
                Files = files.ToArray() ?? Array.Empty<string>();
                ProjectReferences = projectReferences?.ToArray() ?? Array.Empty<string>();
            }

            public string FileName { get; }
            public IEnumerable<string> Files { get; }
            public IEnumerable<string> ProjectReferences { get; }
        }
    }
}
