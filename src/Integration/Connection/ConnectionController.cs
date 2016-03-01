//-----------------------------------------------------------------------
// <copyright file="ConnectionController.cs" company="SonarSource SA and Microsoft Corporation">
//   Copyright (c) SonarSource SA and Microsoft Corporation.  All rights reserved.
//   Licensed under the MIT License. See License.txt in the project root for license information.
// </copyright>
//-----------------------------------------------------------------------

using SonarLint.VisualStudio.Integration.Progress;
using SonarLint.VisualStudio.Integration.Resources;
using SonarLint.VisualStudio.Integration.Service;
using SonarLint.VisualStudio.Integration.TeamExplorer;
using SonarLint.VisualStudio.Integration.WPF;
using SonarLint.VisualStudio.Progress.Controller;
using System;
using System.Diagnostics;

namespace SonarLint.VisualStudio.Integration.Connection
{
    /// <summary>
    /// Connection related controller. 
    /// Provides the following commands:
    /// <see cref="ConnectCommand"/>
    /// <see cref="RefreshCommand"/>
    /// <see cref="DontWarnAgainCommand"/>
    /// </summary>
    internal class ConnectionController : HostedCommandControllerBase, IConnectionInformationProvider, IConnectionWorkflowExecutor
    {
        private readonly IHost host;
        private readonly IConnectionInformationProvider connectionProvider;
        private readonly IIntegrationSettings settings;

        public ConnectionController(IHost host)
            : this(host, null, null)
        {
        }

        internal /*for testing purposes*/ ConnectionController(IHost host, IConnectionInformationProvider connectionProvider, IConnectionWorkflowExecutor workflowExecutor)
            : base(host)
        {
            if (host == null)
            {
                throw new ArgumentNullException(nameof(host));
            }

            this.host = host;
            this.WorkflowExecutor = workflowExecutor ?? this;
            this.connectionProvider = connectionProvider ?? this;
            this.settings = this.host.GetMefService<IIntegrationSettings>();

            this.ConnectCommand = new RelayCommand(this.OnConnect, this.OnConnectStatus);
            this.RefreshCommand = new RelayCommand<ConnectionInformation>(this.OnRefresh, this.OnRefreshStatus);
            this.DontWarnAgainCommand = new RelayCommand(this.OnDontWarnAgain, this.OnDontWarnAgainStatus);
        }

        #region Properties

        public RelayCommand ConnectCommand
        {
            get;
        }

        public RelayCommand DontWarnAgainCommand
        {
            get;
        }

        public RelayCommand<ConnectionInformation> RefreshCommand
        {
            get;
        }

        internal /*for testing purposes*/ IConnectionWorkflowExecutor WorkflowExecutor
        {
            get;
        }

        internal ConnectionInformation LastAttemptedConnection { get; private set; }

        internal bool IsConnectionInProgress
        {
            get
            {
                return this.host.VisualStateManager.IsBusy;
            }
            set
            {
                if (this.host.VisualStateManager.IsBusy != value)
                {
                    this.host.VisualStateManager.IsBusy = value;
                    this.ConnectCommand.RequeryCanExecute();
                    this.RefreshCommand.RequeryCanExecute();
                }
            }
        }
        #endregion

        #region Connect Command

        private bool OnConnectStatus()
        {
            return this.host.SonarQubeService.CurrentConnection == null
                && !this.host.VisualStateManager.IsBusy;
        }

        private void OnConnect()
        {
            Debug.Assert(this.OnConnectStatus());
            Debug.Assert(!this.host.VisualStateManager.IsBusy, "Service is in a connecting state");

            var connectionInfo = this.connectionProvider.GetConnectionInformation(this.LastAttemptedConnection);
            if (connectionInfo != null)
            {
                this.EstablishConnection(connectionInfo);
            }
        }
        #endregion

        #region Refresh Command

        private bool OnRefreshStatus(ConnectionInformation useConnection)
        {
            return !this.host.VisualStateManager.IsBusy
                && (useConnection != null || this.host.SonarQubeService.CurrentConnection != null);
        }

        private void OnRefresh(ConnectionInformation useConnection)
        {
            Debug.Assert(this.OnRefreshStatus(useConnection));

            ConnectionInformation connectionToRefresh = useConnection ?? this.host.SonarQubeService.CurrentConnection;
            Debug.Assert(connectionToRefresh != null, "Expecting either to be connected to get a connection to connect to");

            // Any existing connection will be disconnected and disposed, so create a copy and use it to connect
            this.EstablishConnection(connectionToRefresh.Clone());
        }
        #endregion

        #region Don't warn again command
        private void ShowNuGetWarning(ProgressControllerResult executionResult)
        {
            if (executionResult == ProgressControllerResult.Succeeded && this.settings.ShowServerNuGetTrustWarning)
            {
                this.host.ActiveSection?.UserNotifications?.ShowNotificationWarning(Strings.ServerNuGetTrustWarningMessage, NotificationIds.WarnServerTrustId, this.DontWarnAgainCommand);
            }
        }

        private void OnDontWarnAgain()
        {
            this.settings.ShowServerNuGetTrustWarning = false;
            this.host.ActiveSection?.UserNotifications?.HideNotification(NotificationIds.WarnServerTrustId);
        }

        private bool OnDontWarnAgainStatus()
        {
            return this.settings != null;
        }
        #endregion

        #region IConnectionInformationProvider

        ConnectionInformation IConnectionInformationProvider.GetConnectionInformation(ConnectionInformation currentConnection)
        {
            var dialog = new ConnectionInformationDialog();
            return dialog.ShowDialog(currentConnection);
        }

        #endregion

        #region IConnectionWorkflowExecutor
        private void EstablishConnection(ConnectionInformation connectionInfo)
        {
            Debug.Assert(connectionInfo != null);

            this.LastAttemptedConnection = connectionInfo;

            this.WorkflowExecutor.EstablishConnection(connectionInfo, this.host.VisualStateManager.SetProjects);
        }

        void IConnectionWorkflowExecutor.EstablishConnection(ConnectionInformation information, ConnectedProjectsCallback connectedProjectsChanged)
        {
            ConnectionWorkflow workflow = new ConnectionWorkflow(this.host, this.ConnectCommand, connectedProjectsChanged);
            IProgressEvents progressEvents = workflow.Run(information);
            this.SetConnectionInProgress(progressEvents);
        }

        internal /*for testing purposes*/ void SetConnectionInProgress(IProgressEvents progressEvents)
        {
            this.IsConnectionInProgress = true;

            ProgressNotificationListener progressListener = new ProgressNotificationListener(this.ServiceProvider, progressEvents);
            progressListener.MessageFormat = Strings.ConnectingToSonarQubePrefixMessageFormat;

            progressEvents.RunOnFinished(result =>
            {
                progressListener.Dispose();
                this.IsConnectionInProgress = false;
                this.ShowNuGetWarning(result);
            });
        }
        #endregion
    }
}
