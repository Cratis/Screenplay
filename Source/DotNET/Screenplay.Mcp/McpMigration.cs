// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Migrations;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Migrates the documents of a root from one Screenplay version to another without writing them, and describes a
/// migration the same way for the MCP <c>migrate</c> tool and the CLI's <c>--report</c>.
/// </summary>
static class McpMigration
{
    /// <summary>
    /// The MCP tool name.
    /// </summary>
    internal const string ToolName = "migrate";

    /// <summary>
    /// Migrates the root's documents, or one of them, and returns the result. Nothing is written: a client applies the
    /// returned source itself - the VS Code command replaces the open document's text.
    /// </summary>
    /// <param name="documents">The root's documents.</param>
    /// <param name="arguments">The tool arguments: <c>from</c>, and optionally <c>to</c> and <c>document</c>.</param>
    /// <returns>The report, as <see cref="Describe"/> shapes it.</returns>
    internal static object Read(ImmutableArray<WorkspaceDocument> documents, JsonElement arguments)
    {
        var from = Version(arguments, "from") ?? throw new McpFailure("'from' is required - an old document does not say its version.", -32602);
        var to = Version(arguments, "to") ?? ScreenplayVersion.Current;
        var document = McpJson.OptionalString(arguments, "document") is { } selected ? PlayGlob.Normalize(selected) : null;
        var contents = documents.Select(item => new PlayFileContent(item.Path.Value, item.Text)).ToArray();
        if (document is not null && !contents.Any(content => string.Equals(PlayGlob.Normalize(content.RelativePath), document, StringComparison.Ordinal)))
        {
            throw new McpFailure($"'{document}' is not a document of the root.", -32602);
        }

        var migrator = new ScreenplayMigrator();
        MigrationPlan plan;
        try
        {
            plan = migrator.Plan(from, to);
        }
        catch (InvalidMigrationPlan exception)
        {
            throw new McpFailure(exception.Message, -32602);
        }

        var report = migrator.Migrate(contents, plan, document is null ? null : path => string.Equals(path, document, StringComparison.Ordinal));
        return Describe(report, includeSource: document is not null);
    }

    /// <summary>
    /// Describes a migration as a machine-readable report.
    /// </summary>
    /// <param name="report">The <see cref="MigrationReport"/>.</param>
    /// <param name="includeSource">Whether to include each changed document's migrated text.</param>
    /// <param name="written">The paths that were written, when the caller wrote any.</param>
    /// <returns>The report, ready to serialize.</returns>
    internal static object Describe(MigrationReport report, bool includeSource, IReadOnlySet<string>? written = null) => new
    {
        summary = Summary(report),
        from = report.Plan.From.Major,
        to = report.Plan.To.Major,
        steps = report.Plan.Steps.Select(step => new { from = step.From.Major, to = step.To.Major, name = step.Name.Value }).ToArray(),
        outcome = Outcome(report.Outcome),
        files = report.Files.Select(file => new
        {
            path = file.Path,
            outcome = Outcome(file.Outcome),
            changed = file.Changed,
            written = written?.Contains(file.Path) ?? false,
            failure = file.Failure,
            changes = file.Changes.Select(change => new
            {
                step = change.Step.Value,
                line = change.Change.Location.Line,
                column = change.Change.Location.Column,
                before = change.Change.Before,
                after = change.Change.After
            }).ToArray(),
            reviews = file.Reviews.Select(review => new { line = review.Location.Line, column = review.Location.Column, message = review.Message }).ToArray(),
            diagnostics = file.Diagnostics.Select(diagnostic => new
            {
                code = diagnostic.Code,
                severity = diagnostic.Severity == DiagnosticSeverity.Error ? "error" : "warning",
                line = diagnostic.Location.Line,
                column = diagnostic.Location.Column,
                message = diagnostic.Message
            }).ToArray(),
            source = includeSource && file.Changed ? file.Migrated : null
        }).ToArray()
    };

    /// <summary>
    /// Summarizes a migration in one line - files rewritten, needing review and refused.
    /// </summary>
    /// <param name="report">The <see cref="MigrationReport"/>.</param>
    /// <returns>The summary.</returns>
    internal static string Summary(MigrationReport report)
    {
        var rewritten = report.Files.Count(file => file.Changed && file.Failure is null);
        var review = report.Files.Count(file => file.Outcome == MigrationOutcome.NeedsReview);
        var refused = report.Files.Count(file => file.Outcome == MigrationOutcome.Failed);
        return $"{rewritten} file{(rewritten == 1 ? string.Empty : "s")} rewritten · {review} need{(review == 1 ? "s" : string.Empty)} review · {refused} refused";
    }

    static string Outcome(MigrationOutcome outcome) => outcome switch
    {
        MigrationOutcome.Failed => "failed",
        MigrationOutcome.NeedsReview => "needsReview",
        _ => "migrated"
    };

    static ScreenplayVersion? Version(JsonElement arguments, string name)
    {
        if (McpJson.OptionalString(arguments, name) is not { } text)
        {
            return null;
        }

        return ScreenplayVersion.TryParse(text, out var version)
            ? version
            : throw new McpFailure($"'{name}' must be a Screenplay version such as \"5\" - got '{text}'.", -32602);
    }
}
