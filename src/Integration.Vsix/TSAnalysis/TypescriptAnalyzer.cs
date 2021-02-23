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

        private EslintRulesProvider rulesProvider;

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

        private AnalysisLanguage? lastAnalyzedLanguage;
        private string lastUsedTsConfigFilePath;

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

            if (rulesProvider == null)
            {
                rulesProvider = new EslintRulesProvider(eslintBridge.RuleKeyMapper);
            }

            var language = detectedLanguages.Contains(AnalysisLanguage.Typescript)
                ? AnalysisLanguage.Typescript : AnalysisLanguage.Javascript;

            var rulesChanged = await EnsureCorrectRulesUsedAsync(language);
            var configFilePath = await GetTsConfigFileAsync(language, path, rulesChanged);

            if (configFilePath == null && language == AnalysisLanguage.Typescript)
            {
                logger.WriteLine($"[TS PROTO] No tsconfig.json file found, file will not be analysed: {path}");
                return;
            }
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

        // Returns true if the set of rules used has changed
        private async System.Threading.Tasks.Task<bool> EnsureCorrectRulesUsedAsync(AnalysisLanguage language)
        {
            // Check whether we need to reset the rules
            if (lastAnalyzedLanguage.HasValue && lastAnalyzedLanguage.Value == language)
            {
                return false;
            }

            lastAnalyzedLanguage = language;

//            var rules = GetRulesFromQP(language);
            var rules = GetRules(language);

            await eslintBridge.NewTSConfig();
            await eslintBridge.InitLinter(rules);
            return true;
        }

        private IEnumerable<Rule> GetRulesFromQP(AnalysisLanguage language)
        {
            IEnumerable<Rule> rules;

            switch (language)
            {
                case AnalysisLanguage.Javascript:
                    rules = rulesProvider.JavaScript_SonarWay;
                    rules = rulesProvider.JavaScript_SonarWay_Recommended;
                    break;

                case AnalysisLanguage.Typescript:
                    rules = rulesProvider.TypeScript_SonarWay;
                    rules = rulesProvider.TypeScript_SonarWay_Recommended;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), $"Unsupported language: {language}");
            }

            return rules;
        }

        private IEnumerable<Rule> GetRules(AnalysisLanguage language)
        {
            IEnumerable<string> ruleKeys;

            switch(language)
            {
                case AnalysisLanguage.Javascript:
                    ruleKeys = EslintRulesProvider.GetJavaScriptVsCodeRuleKeys();
                    ruleKeys = EslintRulesProvider.GetJavaScriptRuleKeys();
                    break;

                case AnalysisLanguage.Typescript:
                    ruleKeys = EslintRulesProvider.GetTypeScriptVsCodeRuleKeys();
                    ruleKeys = EslintRulesProvider.GetTypeScriptRuleKeys();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(language), $"Unsupported language: {language}");
            }

            return ruleKeys.Select(x => new Rule { Key = x, Configurations = Array.Empty<string>() })
                .ToArray();
        }

        private async System.Threading.Tasks.Task<string> GetTsConfigFileAsync(AnalysisLanguage language, string sourceFilePath, bool rulesChanged)
        {
            // TODO - set default tsconfig for JavaScript files
            var configFilePath = configMapper.FindTsConfigFile(sourceFilePath);
            
            // Check whether we need to reset the config
            if (!rulesChanged &&
                language == AnalysisLanguage.Typescript
                && lastUsedTsConfigFilePath != configFilePath)
            {
                logger.LogMessage("[TS] Resetting the tsconfig.json...");
                await eslintBridge.NewTSConfig();
            }

            lastUsedTsConfigFilePath = configFilePath;
            return configFilePath;
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
