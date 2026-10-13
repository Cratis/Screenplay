// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

internal sealed partial class McpRecoveryJournal
{
    internal string ReleaseBaselineStage => OperationPath(_root, Record.OperationId, "baseline-stage");
    internal string ReleaseBaselineBackup => OperationPath(_root, Record.OperationId, "baseline-backup");
    internal string ReleaseBaselineRestoreStage => OperationPath(_root, Record.OperationId, "baseline-rollback");

    internal void VerifyReleaseBaseline(bool applied)
    {
        if (Record.ReleaseBaseline is { } baseline) _files.Verify(ReleaseBaselineStore.FileName, applied ? baseline.After : baseline.Before);
    }

    static McpReleaseBaselineJournal? ReleaseBaselineRecord(IMcpProposal proposal, McpManagedFiles files)
    {
        if (proposal is not McpReleaseBaselineProposal baseline) return null;
        var access = baseline.BeforeBaseline is null ? null : McpRecoveryAccess.Capture(".screenplay/release-baseline.json", files.PathFor(ReleaseBaselineStore.FileName));
        return new(baseline.BeforeBaseline, baseline.AfterBaseline, access);
    }

    static IEnumerable<string> ReleaseBaselinePaths(McpRoot root, IMcpProposal proposal, string operationId)
    {
        if (proposal is not McpReleaseBaselineProposal) yield break;
        yield return new McpManagedFiles(root).PathFor(ReleaseBaselineStore.FileName);
        yield return OperationPath(root, operationId, "baseline-stage");
        yield return OperationPath(root, operationId, "baseline-backup");
        yield return OperationPath(root, operationId, "baseline-rollback");
    }

    void ValidateReleaseBaseline()
    {
        if (Record.ReleaseBaseline is not { } baseline) return;
        var before = ReleaseBaselineStore.ReadBytes(baseline.Before, Before.IdentityCatalog.Application);
        var after = ReleaseBaselineStore.ReadBytes(baseline.After, After.IdentityCatalog.Application);
        if (before.Status == "unreadable" || after.Status != "valid" || !baseline.After.AsSpan().SequenceEqual(ReleaseBaselineStore.Serialize(after.Document!)))
        {
            throw new McpFailure("Recovery journal release baseline images are invalid.");
        }

        if ((baseline.Before is null) != (baseline.Access is null) || (baseline.Access is not null && baseline.Access.Path != ".screenplay/release-baseline.json"))
        {
            throw new McpFailure("Recovery journal release baseline access rules are invalid.");
        }

        if (Before.Revision != After.Revision || !McpManagedFiles.Equal(Record.BeforeState, Record.AfterState)) throw new McpFailure("Release baseline recovery must not change model or identity state.");
        baseline.Access?.Validate();
    }

    void CleanupReleaseBaseline()
    {
        if (Record.ReleaseBaseline is not { } baseline) return;
        DeleteKnown(ReleaseBaselineStage, baseline.After);
        DeleteKnown(ReleaseBaselineBackup, baseline.Before);
        DeleteKnown(ReleaseBaselineRestoreStage, baseline.Before);
    }

    void VerifyReleaseBaselineAccess() => Record.ReleaseBaseline?.Access?.Verify(_files.PathFor(ReleaseBaselineStore.FileName));
}
