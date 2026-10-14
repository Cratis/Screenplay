// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Checks or explicitly admits released event revisions using the model root's persisted identities.
/// </summary>
public static class ReleaseBaselineCommands
{
    /// <summary>
    /// Checks the baseline without reconciling or writing identities, retaining removed contracts for verification.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <returns>The clean, violation or unreadable/refused outcome.</returns>
    public static ReleaseBaselineOutcome Check(string root) => Run(root, null, null);

    /// <summary>
    /// Monotonically admits revisions and atomically writes the baseline under the shared recovery journal.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <param name="release">The nonempty, trimmed release label.</param>
    /// <param name="fromEsmPath">Optional canonical shipped ESM file, strictly read with bounded size.</param>
    /// <returns>The written entry count and revision, violations, or an unreadable/refused outcome.</returns>
    public static ReleaseBaselineOutcome Admit(string root, string release, string? fromEsmPath = null) =>
        release is null ? new(2, [], "InvalidAdmission", "Admission requires a release label.") : Run(root, release, fromEsmPath);

    static ReleaseBaselineOutcome Run(string path, string? release, string? fromEsmPath)
    {
        try
        {
            return Execute(new McpRoot(path), release, fromEsmPath);
        }
        catch (Exception exception) when (exception is McpFailure or InvalidSemanticContract or InvalidReleaseBaseline or IOException or UnauthorizedAccessException)
        {
            return new(2, [], exception is McpFailure failure ? failure.FailureKind : exception.GetType().Name, exception.Message);
        }
    }

    static ReleaseBaselineOutcome Execute(McpRoot root, string? release, string? fromEsmPath)
    {
        // The same read ScreenplayModelRoot.OpenForReleaseBaselineCheck and Open perform, refusing as McpFailure.
        var read = McpRootFailures.Run(() => ScreenplayModelRoot.Read(root.Directory, forReleaseBaselineCheck: release is null));
        if (read is null) return new(2, [], ScreenplayModelRoot.PersistedIdentitiesRequired, ScreenplayModelRoot.PersistedIdentitiesRequiredReason);
        var (workspace, _, stateBytes, before, baseline) = read;
        if (release is null) return Outcome(ReleaseBaseline.Check(workspace, baseline));
        var shipped = fromEsmPath is null ? null : ReadShipped(fromEsmPath);
        var admission = ReleaseBaseline.Admit(workspace, baseline, release, DateTimeOffset.UtcNow, shipped);
        var outcome = Outcome(admission);
        if (outcome.ExitCode != 0) return outcome;
        var after = ReleaseBaselineStore.Serialize(admission.Document!);
        var result = new McpDisk(root).Apply(new McpReleaseBaselineProposal(workspace, before, after), new(stateBytes, stateBytes));
        if (!result.Success) return new(2, [], result.FailureKind, $"{result.FailureKind}: {result.Status}; {string.Join("; ", result.Recovery)}");

        return new(0, [], AdmittedEntryCount: admission.Document!.Entries.Length, BaselineRevision: ReleaseBaselineStore.Revision(after));
    }

    static ReleaseBaselineOutcome Outcome(ReleaseBaselineResult result)
    {
        var exitCode = result.Failure is null ? 0 : 2;
        if (!result.Violations.IsEmpty) exitCode = 1;

        return new(exitCode, result.Violations, result.Failure, result.Failure is null ? null : $"{result.Failure}: {result.Reason}");
    }

    static ExecutableSemanticModel ReadShipped(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (stream.Length > McpManagedFiles.MaximumStateBytes) throw new McpFailure("Shipped ESM exceeds the bounded file size.");
        var bytes = new byte[checked((int)stream.Length)];
        stream.ReadExactly(bytes);
        if (stream.ReadByte() != -1) throw new McpFailure("Shipped ESM changed during read.");

        return SemanticModelSerializer.Deserialize(bytes);
    }
}
