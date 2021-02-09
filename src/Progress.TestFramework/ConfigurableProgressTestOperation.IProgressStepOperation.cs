/*
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

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using SonarLint.VisualStudio.Progress.Controller;

namespace SonarLint.VisualStudio.Progress.UnitTests
{
    /// <summary>
    /// Partial implementation of <see cref="IProgressStepOperation"/>
    /// </summary>
    public partial class ConfigurableProgressTestOperation : IProgressStepOperation
    {
        IProgressStep IProgressStepOperation.Step
        {
            get { return this; }
        }

        Task<StepExecutionState> IProgressStepOperation.RunAsync(CancellationToken cancellationToken, IProgressStepExecutionEvents executionNotify)
        {
            cancellationToken.Should().NotBeNull("cancellationToken is not expected to be null");
            executionNotify.Should().NotBeNull("executionNotify is not expected to be null");
            return Task.Factory.StartNew(() =>
            {
                this.ExecutionState = StepExecutionState.Executing;
                this.operation(cancellationToken, executionNotify);
                this.IsExecuted = true;
                return this.ExecutionState = this.ExecutionResult;
            });
        }
    }
}