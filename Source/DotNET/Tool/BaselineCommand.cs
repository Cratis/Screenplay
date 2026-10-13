// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool;

static class BaselineCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var options = Parse(args, error);
        if (options is null) return 2;
        try
        {
            return Execute(options, output, error);
        }
        catch (Exception exception) when (exception is McpFailure or InvalidSemanticContract or InvalidReleaseBaseline or IOException or UnauthorizedAccessException)
        {
            return Fail(error, exception.Message);
        }
    }

    static int Execute(BaselineOptions options, TextWriter output, TextWriter error)
    {
        var root = new McpRoot(options.Root);
        McpRecoveryJournal.RefusePending(root);
        var files = new McpManagedFiles(root);
        var stateBytes = files.Read(McpState.FileName);
        if (stateBytes is null) return Fail(error, "PersistedIdentitiesRequired: release baselines require .screenplay/identities.json.");
        var state = McpState.Deserialize(stateBytes);
        var workspace = options.Action == "check" ? state.OpenForReleaseBaselineCheck(root) : state.Open(root);
        var before = files.Read(ReleaseBaselineStore.FileName, ReleaseBaselineStore.MaximumBytes);
        var baseline = ReleaseBaselineStore.ReadBytes(before, workspace.IdentityCatalog.Application);
        if (options.Action == "check") return Report(ReleaseBaseline.Check(workspace, baseline), output, error);
        var admission = Admit(options, workspace, baseline, output, error, out var verdict);
        if (admission is null) return verdict;
        var after = ReleaseBaselineStore.Serialize(admission.Document!);
        var result = new McpDisk(root).Apply(new McpReleaseBaselineProposal(workspace, before, after), new(stateBytes, stateBytes));
        if (!result.Success) return Fail(error, $"{result.FailureKind}: {result.Status}; {string.Join("; ", result.Recovery)}");
        output.WriteLine($"Admitted {admission.Document!.Entries.Length} event revision(s) in release baseline {ReleaseBaselineStore.Revision(after)}.");
        return 0;
    }

    static ReleaseBaselineResult? Admit(BaselineOptions options, ScreenplayWorkspace workspace, ReleaseBaselineRead baseline, TextWriter output, TextWriter error, out int verdict)
    {
        var shipped = options.FromEsm is null ? null : ReadShipped(options.FromEsm);
        var admission = ReleaseBaseline.Admit(workspace, baseline, options.Release!, DateTimeOffset.UtcNow, shipped);
        verdict = Report(admission, output, error);
        return verdict == 0 ? admission : null;
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

    static int Report(ReleaseBaselineResult result, TextWriter output, TextWriter error)
    {
        foreach (var violation in result.Violations) output.WriteLine($"{violation.Kind}: {violation.Event} revision {violation.Revision}{(violation.Path is null ? string.Empty : " at " + violation.Path)}");
        if (result.Failure is not null && result.Violations.IsEmpty) return Fail(error, $"{result.Failure}: {result.Reason}");
        if (result.Failure is not null) error.WriteLine($"{result.Failure}: {result.Reason}");

        return result.Violations.IsEmpty ? 0 : 1;
    }

    static int Fail(TextWriter error, string reason)
    {
        error.WriteLine(reason);
        return 2;
    }

    static BaselineOptions? Parse(string[] args, TextWriter error)
    {
        if (!KnownAction(args) || !TryReadAdmitOptions(args, out var release, out var from)) return Invalid(error);
        if (args[1] == "admit" && string.IsNullOrWhiteSpace(release)) return Invalid(error);
        return new(args[1], args[2], release, from);
    }

    static bool KnownAction(string[] args)
    {
        if (args.Length < 3 || args[0] != "baseline") return false;
        return args[1] == "admit" || args[1] == "check";
    }

    static bool TryReadAdmitOptions(string[] args, out string? release, out string? from)
    {
        release = null;
        from = null;
        for (var index = 3; index < args.Length; index++)
        {
            var option = args[index];
            if (args[1] != "admit") return false;
            if (++index >= args.Length || args[index].StartsWith("--", StringComparison.Ordinal)) return false;
            if (option == CliCommandCatalog.Release.Name && release is null) release = args[index];
            else if (option == CliCommandCatalog.FromEsm.Name && from is null) from = args[index];
            else return false;
        }

        return true;
    }

    static BaselineOptions? Invalid(TextWriter error)
    {
        error.WriteLine(CliCommandCatalog.BaselineAdmit.Usage);
        error.WriteLine(CliCommandCatalog.BaselineCheck.Usage);
        return null;
    }

    sealed record BaselineOptions(string Action, string Root, string? Release, string? FromEsm);
}
