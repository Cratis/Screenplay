// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpDisk
{
    static void StageReleaseBaseline(McpRecoveryJournal journal)
    {
        if (journal.Record.ReleaseBaseline is { } baseline) McpManagedFiles.WritePrivate(journal.ReleaseBaselineStage, baseline.After);
    }

    // The identity state is not installed separately when the admission journals a release baseline.
    void InstallIdentityState(McpManagedFiles files, McpStatePlan state, McpRecoveryJournal journal)
    {
        if (journal.Record.ReleaseBaseline is null) InstallState(files, state, journal);
    }

    void InstallReleaseBaseline(McpManagedFiles files, McpRecoveryJournal journal)
    {
        if (journal.Record.ReleaseBaseline is not { } baseline) return;
        journal.VerifyMarker();
        journal.VerifyReleaseBaseline(applied: false);
        if (baseline.Before is not null)
        {
            _move(files.PathFor(ReleaseBaselineStore.FileName), journal.ReleaseBaselineBackup);
            McpFileAccess.Preserve(journal.ReleaseBaselineBackup, journal.ReleaseBaselineStage);
        }

        if (!McpManagedFiles.Equal(McpManagedFiles.ReadPath(journal.ReleaseBaselineStage, ReleaseBaselineStore.MaximumBytes), baseline.After))
        {
            throw new McpFailure("Staged release baseline changed before installation.") { FailureKind = "ReleaseBaselineRevisionDrift" };
        }

        _move(journal.ReleaseBaselineStage, files.PathFor(ReleaseBaselineStore.FileName));
        journal.VerifyReleaseBaseline(applied: true);
    }
}
