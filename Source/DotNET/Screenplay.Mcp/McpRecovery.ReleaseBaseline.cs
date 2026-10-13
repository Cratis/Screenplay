// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecovery
{
    void PreflightReleaseBaseline()
    {
        if (journal.Record.ReleaseBaseline is not { } baseline) return;
        _ = ReadKnown(_files.PathFor(ReleaseBaselineStore.FileName), baseline.Before, baseline.After);
        CheckArtifact(journal.ReleaseBaselineStage, baseline.After);
        CheckArtifact(journal.ReleaseBaselineBackup, baseline.Before);
        CheckArtifact(journal.ReleaseBaselineRestoreStage, baseline.Before);
    }

    void RestoreReleaseBaseline()
    {
        if (journal.Record.ReleaseBaseline is not { } baseline) return;
        journal.VerifyMarker();
        var path = _files.PathFor(ReleaseBaselineStore.FileName);
        var actual = ReadKnown(path, baseline.Before, baseline.After);
        if (McpManagedFiles.Equal(actual, baseline.Before))
        {
            baseline.Access?.Restore(path);
            return;
        }

        if (actual is not null) File.Delete(path);
        if (baseline.Before is not null)
        {
            StageOriginal(journal.ReleaseBaselineRestoreStage, baseline.Before);
            baseline.Access!.Restore(journal.ReleaseBaselineRestoreStage);
            File.Move(journal.ReleaseBaselineRestoreStage, _files.PathFor(ReleaseBaselineStore.FileName));
        }

        journal.VerifyReleaseBaseline(applied: false);
    }
}
