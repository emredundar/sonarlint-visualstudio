﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2019 SonarSource SA
 * mailto:info AT sonarsource DOT com
 *
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this program; if not, write to the Free Software Foundation,
 * Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
 */

using System;
using System.IO;
using SonarLint.VisualStudio.Integration;

namespace SonarJsConfig
{
    /// <summary>
    /// Downloads the Sonar JavaScript/TypeScript jar and extracts the files that need to be
    /// embedded in the SLVS vsix
    /// </summary>
    /// <remarks>
    /// Assumptions:
    /// * we are downloading the sonar-js plugin jar e.g. https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar
    /// * the jar contains a .tgz archive containing ESLintBridge (a NodeJS app)
    /// * the jar contains various .json files, some with fixed names, some that we'll match using wildcards
    /// 
    /// We want the task to fail if it cannot locate all of the expected files (otherwise we'll build an invalid VSIX).
    /// 
    /// Downloading and extracting the files can be slow so we want to skip those steps if possible.
    /// </remarks>
    public class SonarJSDownloader
    {
        // The archive containing the eslintbridge
        private const string EsLintBridgeFilePattern = "eslint-bridge-*.tgz";

        // eslint-bridge-1.0.0.tgz -> eslint-bridge-1.0.0.tar ->
        //  package\bin\
        //  package\lib\
        //  package\node_modules\
        //  package\package.json

        // Sub-folder into which the tar file should be unzipped
        public const string EslintBridgeFolderName = "eslint-bridge";

        // List of patterns to match single files in the uncompressed output
        private readonly string[] SingleFilePatterns = new string[]
        {
                "Sonar_way_profile.json",
                "Sonar_way_recommended_profile.json"
        };

        // List of patterns to match multiple files in the uncompressed output
        private readonly string[] MultipleFilesPatterns = new string[]
        {
            @"org\sonar\l10n\javascript\rules\eslint\*.json",
            @"org\sonar\l10n\javascript\rules\javascript\*.json",
            @"org\sonar\l10n\typescript\rules\tslint\*.json",
            @"org\sonar\l10n\typescript\rules\tslint-sonarts\*.json",
            EslintBridgeFolderName + @"\package\*"

        };

        /// <summary>
        /// Downloads the specific SonarJS plugin and return the directory to which it was downloaded
        /// </summary>
        /// <returns></returns>
        // Download url example: https://binaries.sonarsource.com/Distribution/sonar-javascript-plugin/sonar-javascript-plugin-6.2.0.12043.jar
        public string Download(string downloadUrl, ILogger logger)
        {
            var pluginFileName = Common.ExtractPluginFileNameFromUrl(downloadUrl, logger);

            // Ensure working directories exists
            var localWorkingFolder = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData"), "SLVS_Internal_Build", "SonarJS");
            var perVersionPluginFolder = Path.Combine(localWorkingFolder, Path.GetFileNameWithoutExtension(pluginFileName));
            Common.EnsureWorkingDirectoryExist(perVersionPluginFolder, logger);

            // Download and unzip the jar
            var jarFilePath = Path.Combine(perVersionPluginFolder, pluginFileName);
            Common.DownloadJarFile(downloadUrl, jarFilePath, logger);
            Common.UnzipJar(jarFilePath, perVersionPluginFolder, logger);

            // Note: not required just to fetch the rules config.
            // However, it will unpack the eslint bridge
            // Uncompress and extract the windows tar archive to get the eslint-bridge folder
            var tarFilePath = Common.FindSingleFile(perVersionPluginFolder, EsLintBridgeFilePattern, logger);
            var tarSubFolder = Path.Combine(perVersionPluginFolder, EslintBridgeFolderName);
            Common.UncompressAndUnzipTgz(tarFilePath, tarSubFolder, logger);

            //// Locate the required files from the uncompressed jar and tar
            //var fileList = Common.FindSingleFiles(perVersionPluginFolder, SingleFilePatterns, logger);
            //var multipleFileList = Common.FindMultipleFiles(perVersionPluginFolder, MultipleFilesPatterns, logger);

            //var FilesToEmbed = fileList.Union(multipleFileList).ToArray();

            return perVersionPluginFolder;
        }
    }
}
