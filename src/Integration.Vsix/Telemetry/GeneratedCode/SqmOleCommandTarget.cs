﻿// <copyright file="SqmCommandFacade.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>

// <auto-generated>
// This code was generated using text template tool.
//
// Changes to this file may cause incorrect behavior and will be lost if
// the code is regenerated.
// </auto-generated>
//
// This file contains a class that handles the SQM pseudo-commands.

using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using System;
using System.ComponentModel;
using OLEConstants = Microsoft.VisualStudio.OLE.Interop.Constants;

namespace SonarLint.VisualStudio.Integration
{

    internal enum SonarLintSqmCommandIds
    {
        BoundSolutionDetectedCommandId = 0x200,
        ConnectCommandCommandId = 0x300,
        BindCommandCommandId = 0x301,
        BrowseToUrlCommandCommandId = 0x302,
        BrowseToProjectDashboardCommandCommandId = 0x303,
        RefreshCommandCommandId = 0x304,
        DisconnectCommandCommandId = 0x305,
        ToggleShowAllProjectsCommandCommandId = 0x306,
        DontWarnAgainCommandCommandId = 0x307,
        FixConflictsCommandCommandId = 0x308,
        FixConflictsShowCommandId = 0x309,
        ErrorListInfoBarShowCommandId = 0x400,
        ErrorListInfoBarUpdateCommandCommandId = 0x401,
    }

    /// <summary>
    /// Implementation for the IOleCommandTarget
    /// to log SQM data for the SonarLint feature.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    internal class SonarLintSqmCommandTarget : IOleCommandTarget
    {
        #region Fields
        internal static readonly Guid CommandSetIdentifier = new Guid("{DB0701CC-1E44-41F7-97D6-29B160A70BCB}");
        #endregion

        #region IOleCommandTarget
        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (IsSqmCommand(pguidCmdGroup, (int)nCmdID))
            {
                 return VSConstants.S_OK;
            }
            return (int)OLEConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            // This handler enables ALL psuedo commands for logging SQM data.
            // i.e. all commands in guid group SonarLintSqmCommandTarget.CommandSetIdentifier
            // are psuedo commands for logging SQM and this handler will enable them when fired.

            int commandId = (int)prgCmds[0].cmdID;
            if (IsSqmCommand(pguidCmdGroup, commandId))
            {
                prgCmds[0].cmdf =
                    (uint)OLECMDF.OLECMDF_SUPPORTED |
                    (uint)OLECMDF.OLECMDF_ENABLED;
                return VSConstants.S_OK;
            }
            return (int)OLEConstants.OLECMDERR_E_NOTSUPPORTED;
        }

        #endregion

        /// </summary>
        /// Returns true if the specified command is a recognised SQM command, otherwise false.
        /// </summary>
        public static bool IsSqmCommand(Guid commandGroup, int commandId)
        {
            if (commandGroup == SonarLintSqmCommandTarget.CommandSetIdentifier)
            {
                SonarLintSqmCommandIds sqmCommandId = (SonarLintSqmCommandIds)commandId;
                switch (sqmCommandId)
                {
                    case SonarLintSqmCommandIds.BoundSolutionDetectedCommandId:
                    case SonarLintSqmCommandIds.ConnectCommandCommandId:
                    case SonarLintSqmCommandIds.BindCommandCommandId:
                    case SonarLintSqmCommandIds.BrowseToUrlCommandCommandId:
                    case SonarLintSqmCommandIds.BrowseToProjectDashboardCommandCommandId:
                    case SonarLintSqmCommandIds.RefreshCommandCommandId:
                    case SonarLintSqmCommandIds.DisconnectCommandCommandId:
                    case SonarLintSqmCommandIds.ToggleShowAllProjectsCommandCommandId:
                    case SonarLintSqmCommandIds.DontWarnAgainCommandCommandId:
                    case SonarLintSqmCommandIds.FixConflictsCommandCommandId:
                    case SonarLintSqmCommandIds.FixConflictsShowCommandId:
                    case SonarLintSqmCommandIds.ErrorListInfoBarShowCommandId:
                    case SonarLintSqmCommandIds.ErrorListInfoBarUpdateCommandCommandId:
                    {
                        return true;
                    }
                }
            }
            return false;

        }

    }

}

