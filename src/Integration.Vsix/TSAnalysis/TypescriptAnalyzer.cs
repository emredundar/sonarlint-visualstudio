using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using SonarJsConfig;
using SonarJsConfig.Config;
using SonarJsConfig.ESLint.Data;
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
        private readonly IEslintBridge eslintBridge;
        private readonly ITsConfigMapper configMapper;
        private readonly ILogger logger;

        [ImportingConstructor]
        public TypescriptAnalyzer(IEslintBridge eslintBridge, ITsConfigMapper configMapper, ITsConfigMonitor monitor, ILogger logger)
        {
            // We're creating the monitor here so that it starts listening for changes.
            // We don't actually need to call it.

            this.eslintBridge = eslintBridge;
            this.configMapper = configMapper;
            this.logger = logger;
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

            ThreadHelper.JoinableTaskFactory.RunAsync(() => InternalExecuteAnalysisAsync(path, consumer, detectedLanguages));
        }

        private async System.Threading.Tasks.Task InternalExecuteAnalysisAsync(string path,
            IIssueConsumer consumer,
            IEnumerable<AnalysisLanguage> detectedLanguages)
        {
            //// See https://github.com/microsoft/vs-threading/blob/master/doc/cookbook_vs.md for
            //// info on VS threading.
            //await System.Threading.Tasks.Task.Yield(); // Get off the caller's callstack
            if (ThreadHelper.CheckAccess())
            {
                // Switch a background thread
                await TaskScheduler.Default;
            }

            var serverStarted = await eslintBridge.Start();
            if (!serverStarted)
            {
                return;
            }

            var configFilePath = configMapper.FindTsConfigFile(path);
            if (configFilePath == null)
            {
                logger.WriteLine($"[TS PROTO] No tsconfig.json file found, file will not be analysed: {path}");
            }

            var language = detectedLanguages.Contains(AnalysisLanguage.Typescript)
                ? AnalysisLanguage.Typescript : AnalysisLanguage.Javascript;

            var rules = GetRules(language);
            await eslintBridge.InitLinter(rules);

            var fileContent = string.Empty; //GetFileContent(projectItem);

            AnalysisResponse response;
            var ignoreHeaderComments = false;
            if (language == AnalysisLanguage.Typescript)
            {
                response = await eslintBridge.AnalyzeTS(path, fileContent, ignoreHeaderComments);
            }
            else
            {
                response = await eslintBridge.AnalyzeJS(path, fileContent, ignoreHeaderComments);
            }

            if (!response.Issues.Any())
            {
                return;
            }

            var timer = Stopwatch.StartNew();

            var analysisIssues = response.Issues.Select(x =>
                new Sonarlint.Issue
                {
                    EndLine = x.EndLine,
                    Message = x.Message,
                    RuleKey = $"{language.ToString().ToLowerInvariant()}:{x.RuleId}",
                    StartLine = x.Line,
                    FilePath = path,
                    StartLineOffset = x.Column,
                    EndLineOffset = x.EndColumn
                });

            consumer.Accept(path, analysisIssues);
            timer.Stop();

            logger.WriteLine($"Number of issues returned: {analysisIssues.Count()}");
            logger.WriteLine($"Time for consumer to process issues: {timer.ElapsedMilliseconds}ms");
        }

        private IEnumerable<Rule> GetRules(AnalysisLanguage language)
        {
            IEnumerable<string> ruleKeys;

            switch(language)
            {
                case AnalysisLanguage.Javascript:
                    ruleKeys = EslintRulesProvider.GetJavaScriptRuleKeys();
                    break;

                case AnalysisLanguage.Typescript:
                    ruleKeys = EslintRulesProvider.GetTypeScriptRuleKeys();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), $"Unsupported language: {language}");
            }

            return ruleKeys.Select(x => new Rule { Key = x, Configurations = Array.Empty<string>() })
                .ToArray();
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
    }
}
