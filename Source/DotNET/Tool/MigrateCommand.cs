// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
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

    internal static int Run(string[] arguments, TextWriter output, TextWriter error) => Run(arguments, output, error, WriteFile);

    /// <summary>
    /// Runs the command, writing each migrated file with the given writer.
    /// </summary>
    /// <param name="arguments">The command-line arguments after <c>migrate</c>.</param>
    /// <param name="output">Where the report goes.</param>
    /// <param name="error">Where errors go.</param>
    /// <param name="writeFile">Writes a migrated text to a file, keeping the file's encoding.</param>
    /// <returns>The exit code.</returns>
    internal static int Run(string[] arguments, TextWriter output, TextWriter error, Action<string, string> writeFile)
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

            var (written, failure) = options.DryRun ? ([], null) : Write(options.Target, report, writeFile);
            Print(report, options, output);
            if (options.Report is not null)
            {
                File.WriteAllText(options.Report, JsonSerializer.Serialize(McpMigration.Describe(report, includeSource: false, written), _json));
            }

            if (failure is not null)
            {
                // Say exactly what is on disk now, so the author knows which files to check or restore.
                var done = written.Count == 0 ? "No file was written." : $"Written before the failure: {string.Join(", ", written.Order(StringComparer.Ordinal))}.";
                var left = report.Files.Where(file => file.Changed && file.Failure is null && !written.Contains(file.Path) && file.Path != failure.Value.Path).Select(file => file.Path).ToArray();
                var rest = left.Length == 0 ? string.Empty : $" Not written: {string.Join(", ", left)}.";
                error.WriteLine($"Could not write '{failure.Value.Path}': {failure.Value.Message} {done}{rest}");
                return Failure;
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
    // Writing stops at the first file that cannot be written; the files before it are written and reported.
    static (HashSet<string> Written, (string Path, string Message)? Failure) Write(string target, MigrationReport report, Action<string, string> writeFile)
    {
        var root = File.Exists(target) ? Path.GetDirectoryName(Path.GetFullPath(target))! : target;
        var written = new HashSet<string>(StringComparer.Ordinal);
        foreach (var file in report.Files.Where(file => file.Changed && file.Failure is null))
        {
            try
            {
                writeFile(Path.Combine(root, file.Path), file.Migrated);
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
            {
                return (written, (file.Path, exception.Message));
            }

            written.Add(file.Path);
        }

        return (written, null);
    }

    // Keeps the encoding the file was read with - a byte order mark included - so only the migrated lines change. The
    // text goes to a file beside it first and then replaces it, so a failed write never leaves half a file.
    static void WriteFile(string path, string text)
    {
        var encoding = EncodingOf(path);
        var temporary = $"{path}.migrating";
        try
        {
            File.WriteAllText(temporary, text, encoding);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    static Encoding EncodingOf(string path)
    {
        Span<byte> start = stackalloc byte[4];
        int read;
        using (var stream = File.OpenRead(path))
        {
            read = stream.ReadAtLeast(start, start.Length, throwOnEndOfStream: false);
        }

        var bytes = start[..read];
        return bytes switch
        {
            [0xEF, 0xBB, 0xBF, ..] => new UTF8Encoding(encoderShouldEmitUTF8Identifier: true),
            [0xFF, 0xFE, 0x00, 0x00] => new UTF32Encoding(bigEndian: false, byteOrderMark: true),
            [0xFF, 0xFE, ..] => new UnicodeEncoding(bigEndian: false, byteOrderMark: true),
            [0xFE, 0xFF, ..] => new UnicodeEncoding(bigEndian: true, byteOrderMark: true),
            _ => new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)
        };
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
