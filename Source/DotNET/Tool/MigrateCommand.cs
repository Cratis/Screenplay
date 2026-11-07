// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Contracts;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Mcp;
using Cratis.Screenplay.Migrations;

namespace Cratis.Screenplay.Tool;

/// <summary>
/// Migrates a model from one Screenplay version to another: <c>screenplay migrate &lt;path&gt; --from &lt;version&gt;
/// [--to &lt;version&gt;] [--dry-run] [--report &lt;file&gt;]</c>. Exits 0 when everything migrated, 2 when a file needs
/// review and 1 on failure.
/// </summary>
static class MigrateCommand
{
    const int Migrated = 0;
    const int Failure = 1;
    const int NeedsReview = 2;
    static readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static int Run(string[] arguments, TextWriter output, TextWriter error)
    {
        if (!TryRead(arguments, error, out var options))
        {
            return Failure;
        }

        var migrator = new ScreenplayMigrator();
        MigrationPlan plan;
        try
        {
            plan = migrator.Plan(options.From, options.To);
        }
        catch (InvalidMigrationPlan exception)
        {
            error.WriteLine(exception.Message);
            return Failure;
        }

        try
        {
            var report = migrator.MigratePath(options.Target, plan);
            if (report.Files.Count == 0)
            {
                error.WriteLine($"No .play files found at '{options.Target}'.");
                return Failure;
            }

            var written = options.DryRun ? new HashSet<string>(StringComparer.Ordinal) : Write(options.Target, report);
            Print(report, options, output);
            if (options.Report is not null)
            {
                File.WriteAllText(options.Report, JsonSerializer.Serialize(McpMigration.Describe(report, includeSource: false, written), _json));
            }

            return report.Outcome switch
            {
                MigrationOutcome.Failed => Failure,
                MigrationOutcome.NeedsReview => NeedsReview,
                _ => Migrated
            };
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            error.WriteLine($"Could not migrate '{options.Target}': {exception.Message}");
            return Failure;
        }
    }

    // A document that does not compile after the migration is still written, so the rest can be fixed by hand; a
    // document a step could not rewrite is left as it was.
    static HashSet<string> Write(string target, MigrationReport report)
    {
        var root = File.Exists(target) ? Path.GetDirectoryName(Path.GetFullPath(target))! : target;
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in report.Files.Where(file => file.Changed && file.Failure is null))
        {
            File.WriteAllText(Path.Combine(root, file.Path), file.Migrated);
            written.Add(file.Path);
        }

        return written;
    }

    static void Print(MigrationReport report, Options options, TextWriter output)
    {
        var steps = report.Plan.Steps.Count;
        output.WriteLine($"Migrating {report.Files.Count} file{(report.Files.Count == 1 ? string.Empty : "s")} from Screenplay {report.Plan.From} to {report.Plan.To} ({steps} step{(steps == 1 ? string.Empty : "s")}: {report.Plan.Description})");
        foreach (var file in report.Files.Where(file => file.Changes.Count > 0 || file.Outcome != MigrationOutcome.Migrated))
        {
            output.WriteLine(file.Path);
            if (file.Failure is not null)
            {
                output.WriteLine($"  refused: {file.Failure}");
            }

            foreach (var change in file.Changes)
            {
                output.WriteLine($"  {change.Change.Location.Line}:{change.Change.Location.Column} {change.Change.Before} → {change.Change.After}");
            }

            foreach (var review in file.Reviews)
            {
                output.WriteLine($"  {review.Location.Line}:{review.Location.Column} needs review: {review.Message}");
            }

            foreach (var diagnostic in file.Diagnostics)
            {
                var severity = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning";
                output.WriteLine($"  {diagnostic.Location.Line}:{diagnostic.Location.Column} {severity} {diagnostic.Code}: {diagnostic.Message}");
            }
        }

        output.WriteLine(McpMigration.Summary(report));
        if (options.DryRun)
        {
            output.WriteLine("Dry run: nothing was written. Run again without --dry-run to write.");
        }

        if (report.Outcome == MigrationOutcome.NeedsReview)
        {
            output.WriteLine("Exit code 2: review needed.");
        }
    }

    static bool TryRead(string[] arguments, TextWriter error, out Options options)
    {
        options = default;
        string? target = null;
        string? from = null;
        string? to = null;
        string? report = null;
        var dryRun = false;
        for (var index = 0; index < arguments.Length; index++)
        {
            var argument = arguments[index];
            if (argument == CliCommandCatalog.DryRun.Name)
            {
                dryRun = true;
            }
            else if (argument == CliCommandCatalog.MigrateFrom.Name || argument == CliCommandCatalog.MigrateTo.Name || argument == CliCommandCatalog.MigrationReport.Name)
            {
                if (++index >= arguments.Length || arguments[index].StartsWith("--", StringComparison.Ordinal))
                {
                    error.WriteLine($"{argument} requires a value. Usage: {CliCommandCatalog.Migrate.Usage}");
                    return false;
                }

                if (argument == CliCommandCatalog.MigrateFrom.Name) from = arguments[index];
                else if (argument == CliCommandCatalog.MigrateTo.Name) to = arguments[index];
                else report = arguments[index];
            }
            else if (target is null && !argument.StartsWith("--", StringComparison.Ordinal))
            {
                target = argument;
            }
            else
            {
                error.WriteLine($"Unexpected argument '{argument}'. Usage: {CliCommandCatalog.Migrate.Usage}");
                return false;
            }
        }

        if (target is null || from is null)
        {
            error.WriteLine($"migrate needs a path and --from - an old file does not say its version. Usage: {CliCommandCatalog.Migrate.Usage}");
            return false;
        }

        if (!ScreenplayVersion.TryParse(from, out var fromVersion))
        {
            error.WriteLine($"--from must be a Screenplay version such as 5 - got '{from}'.");
            return false;
        }

        var toVersion = ScreenplayVersion.Current;
        if (to is not null && !ScreenplayVersion.TryParse(to, out toVersion))
        {
            error.WriteLine($"--to must be a Screenplay version such as 6 - got '{to}'.");
            return false;
        }

        if ((!File.Exists(target) && !Directory.Exists(target)) || (File.Exists(target) && !target.EndsWith(".play", StringComparison.OrdinalIgnoreCase)))
        {
            error.WriteLine($"'{target}' must be an existing .play file or model directory.");
            return false;
        }

        options = new(target, fromVersion, toVersion, dryRun, report);
        return true;
    }

    readonly record struct Options(string Target, ScreenplayVersion From, ScreenplayVersion To, bool DryRun, string? Report);
}
