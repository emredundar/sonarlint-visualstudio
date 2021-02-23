using System;
using System.ComponentModel.Composition;
using System.IO;
using System.Threading.Tasks;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using SonarJsConfig.Config;

namespace SonarLint.VisualStudio.Integration.Vsix.TSAnalysis
{
    public interface ITsConfigMonitor
    {
    }

    [Export(typeof(ITsConfigMonitor))]
    public class TsConfigMonitor : ITsConfigMonitor
    {
        private readonly IActiveSolutionTracker activeSolutionTracker;
        private readonly ITsConfigMapper tsConfigMapper;
        private readonly ILogger logger;

        private readonly IVsSolution solution;

        [ImportingConstructor]
        public TsConfigMonitor([Import(typeof(SVsServiceProvider))]IServiceProvider serviceProvider,
            IActiveSolutionTracker activeSolutionTracker,
            ITsConfigMapper tsConfigMapper,
            ILogger logger)
        {
            this.activeSolutionTracker = activeSolutionTracker;
            this.tsConfigMapper = tsConfigMapper;
            this.logger = logger;

            ThreadHelper.ThrowIfNotOnUIThread();
            solution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;

            SafeUpdateTsMapping();

            activeSolutionTracker.ActiveSolutionChanged += ActiveSolutionTracker_ActiveSolutionChanged;

            // TODO: ActiveSolutionChanged doesn't seem to fire for "Open folder" scenarios
            activeSolutionTracker.AfterProjectOpened += ActiveSolutionTracker_AfterProjectOpened;
        }

        private void ActiveSolutionTracker_AfterProjectOpened(object sender, ProjectOpenedEventArgs e) =>
            SafeUpdateTsMapping();


        private void ActiveSolutionTracker_ActiveSolutionChanged(object sender, ActiveSolutionChangedEventArgs e) =>
            SafeUpdateTsMapping();


        private void SafeUpdateTsMapping()
        {
            try
            {
                logger.WriteLine("[TS PROTO]: clearing the TypeScript config mapping");
                tsConfigMapper.Reset();

                var dir = GetSolutionDirectory();
                if (dir != null && Directory.Exists(dir))
                {
                    logger.WriteLine("[TS PROTO]: updating the TypeScript config mapping");

                    UpdateConfigAsync(dir).Forget();
                }
            }
            catch(Exception ex) when (!ErrorHandler.IsCriticalException(ex))
            {
                logger.WriteLine($"Error in TsConfigMonitor: {ex.Message}");
            }
        }

        private async System.Threading.Tasks.Task UpdateConfigAsync(string baseDirectory)
        {
            if (ThreadHelper.CheckAccess())
            {
                // Switch a background thread
                await TaskScheduler.Default;
            }

            await tsConfigMapper.InitializeAsync(baseDirectory);
        }

        private string GetSolutionDirectory()
        {
            solution.GetSolutionInfo(out var slnDirectory, out var slnFile, out var userOptsFile);

            return slnDirectory;
        }

    }
}
