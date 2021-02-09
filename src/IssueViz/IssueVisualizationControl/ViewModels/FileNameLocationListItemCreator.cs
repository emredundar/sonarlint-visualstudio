﻿/*
 * SonarLint for Visual Studio
 * Copyright (C) 2016-2021 SonarSource SA
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

using Microsoft.VisualStudio.Shell.Interop;
using SonarLint.VisualStudio.Integration;
using SonarLint.VisualStudio.IssueVisualization.Models;

namespace SonarLint.VisualStudio.IssueVisualization.IssueVisualizationControl.ViewModels
{
    internal interface IFileNameLocationListItemCreator
    {
        IFileNameLocationListItem Create(IAnalysisIssueLocationVisualization location);
    }

    internal class FileNameLocationListItemCreator : IFileNameLocationListItemCreator
    {
        private readonly IVsImageService2 vsImageService;
        private readonly ILogger logger;

        public FileNameLocationListItemCreator(IVsImageService2 vsImageService, ILogger logger)
        {
            this.vsImageService = vsImageService;
            this.logger = logger;
        }

        public IFileNameLocationListItem Create(IAnalysisIssueLocationVisualization location)
        {
            return new FileNameLocationListItem(location, vsImageService, logger);
        }
    }
}
