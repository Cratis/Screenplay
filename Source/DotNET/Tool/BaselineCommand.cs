// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Mcp;

namespace Cratis.Screenplay.Tool;

static class BaselineCommand
{
    internal static int Run(string[] args, TextWriter output, TextWriter error)
    {
        var options = Parse(args, error);
        if (options is null) return 2;
        var outcome = options.Action == "check"
            ? ReleaseBaselineCommands.Check(options.Root)
            : ReleaseBaselineCommands.Admit(options.Root, options.Release!, options.FromEsm);

        return Report(outcome, output, error);
    }

    static int Report(ReleaseBaselineOutcome result, TextWriter output, TextWriter error)
    {
        foreach (var violation in result.Violations) output.WriteLine($"{violation.Kind}: {violation.Event} revision {violation.Revision}{(violation.Path is null ? string.Empty : " at " + violation.Path)}");
        if (result.Reason is not null) error.WriteLine(result.Reason);
        if (result.AdmittedEntryCount is { } count) output.WriteLine($"Admitted {count} event revision(s) in release baseline {result.BaselineRevision}.");

        return result.ExitCode;
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
