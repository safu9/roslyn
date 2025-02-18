﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Notification;
using Microsoft.CodeAnalysis.Shared.Utilities;
using Roslyn.Utilities;

namespace Microsoft.CodeAnalysis.ChangeSignature
{
    /// <summary>
    /// Defines the <see cref="CodeActionOperation"/> for the <see cref="ChangeSignatureCodeAction"/>
    /// This is used instead of <see cref="ApplyChangesOperation"/> as we need to show a confirmation
    /// dialog to the user before applying the change.
    /// </summary>
    internal sealed class ChangeSignatureCodeActionOperation : CodeActionOperation
    {
        public Solution ChangedSolution { get; }

        public string? ConfirmationMessage { get; }

        public ChangeSignatureCodeActionOperation(Solution changedSolution, string? confirmationMessage)
        {
            ChangedSolution = changedSolution ?? throw new ArgumentNullException(nameof(changedSolution));
            ConfirmationMessage = confirmationMessage;
        }

        internal override bool ApplyDuringTests => true;

        public sealed override void Apply(Workspace workspace, CancellationToken cancellationToken)
            => ApplyWorker(workspace, new ProgressTracker());

        /// <summary>
        /// Show the confirmation message, if available, before attempting to apply the changes.
        /// </summary>
        internal sealed override Task<bool> TryApplyAsync(
            Workspace workspace, IProgressTracker progressTracker, CancellationToken cancellationToken)
        {
            return ApplyWorker(workspace, progressTracker) ? SpecializedTasks.True : SpecializedTasks.False;
        }

        private bool ApplyWorker(Workspace workspace, IProgressTracker progressTracker)
        {
            if (ConfirmationMessage != null)
            {
                var notificationService = workspace.Services.GetRequiredService<INotificationService>();
                if (!notificationService.ConfirmMessageBox(ConfirmationMessage, severity: NotificationSeverity.Warning))
                {
                    return false;
                }
            }

            return workspace.TryApplyChanges(ChangedSolution, progressTracker);
        }
    }
}
