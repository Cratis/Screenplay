// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// What the event model board view is given: the application's documents, and what a change would make of them.
/// </summary>
static class McpVisualization
{
    /// <summary>The name of the tool that shows the board.</summary>
    internal const string ToolName = "visualize-model";

    // Declarations a reader of an event model sees as boxes and lanes; the rest are details of those.
    static readonly HashSet<string> _drawnKinds = new(StringComparer.Ordinal) { "Module", "Feature", "Slice", "Command", "Event", "ReadModel", "Screen", "Reaction" };

    /// <summary>
    /// The schema of a sketch: whole .play documents laid over the documents on disk.
    /// </summary>
    /// <returns>The JSON schema.</returns>
    internal static JsonObject SketchSchema()
    {
        var document = McpAstSchemas.Object(new JsonObject { ["path"] = McpAstSchemas.String(), ["source"] = McpAstSchemas.String() }, "path", "source");
        var schema = McpAstSchemas.Array(document);
        schema["minItems"] = 1;
        schema["maxItems"] = McpRoot.MaximumFiles;
        return schema;
    }

    /// <summary>
    /// Builds the tool result. The documents go only to the view, as structured content; the model is
    /// told what the board shows and, for a change, which declarations it adds and removes.
    /// </summary>
    /// <param name="application">The application's name.</param>
    /// <param name="current">The documents as they are.</param>
    /// <param name="proposed">The documents as the change would leave them, if any.</param>
    /// <param name="proposalId">The proposal the change comes from, if any.</param>
    /// <returns>The tools/call result.</returns>
    internal static object Result(string application, ImmutableArray<WorkspaceDocument> current, ImmutableArray<WorkspaceDocument>? proposed, string? proposalId)
    {
        var before = Describe(current);
        var after = proposed is { } documents ? Describe(documents) : null;
        var added = after is null ? [] : after.Declarations.Except(before.Declarations).Order(StringComparer.Ordinal).ToArray();
        var removed = after is null ? [] : before.Declarations.Except(after.Declarations).Order(StringComparer.Ordinal).ToArray();
        var structured = new
        {
            application,
            documents = current.Select(document => new { path = document.Path.Value, source = document.Text }),
            changes = proposed is { } changed ? Changes(current, changed) : null,
            proposalId,
            current = before.Summary,
            proposed = after?.Summary,
            added,
            removed
        };
        return new
        {
            content = new[] { new { type = "text", text = Text(application, current.Length, before, after, added, removed, proposalId) } },
            structuredContent = structured,
            isError = false
        };
    }

    static object[] Changes(ImmutableArray<WorkspaceDocument> current, ImmutableArray<WorkspaceDocument> proposed)
    {
        var before = current.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var after = proposed.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var changes = new List<object>();
        foreach (var (path, source) in after.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            if (!before.TryGetValue(path, out var existing) || existing != source)
            {
                changes.Add(new { path, source });
            }
        }

        // A document the change takes away is listed without source.
        changes.AddRange(before.Keys.Where(path => !after.ContainsKey(path)).Order(StringComparer.Ordinal).Select(path => new { path }));
        return [.. changes];
    }

    static Description Describe(ImmutableArray<WorkspaceDocument> documents)
    {
        var snapshot = new McpSnapshot(documents);
        var declarations = snapshot.Index.Declarations.Where(declaration => _drawnKinds.Contains(declaration.Kind))
            .Select(declaration => $"{declaration.Kind} {declaration.Address}")
            .ToHashSet(StringComparer.Ordinal);
        var diagnostics = snapshot.Compilation.Diagnostics.ToArray();
        var summary = new Summary(
            declarations.Count(declaration => declaration.StartsWith("Slice ", StringComparison.Ordinal)),
            declarations.Count(declaration => declaration.StartsWith("Event ", StringComparison.Ordinal)),
            declarations.Count(declaration => declaration.StartsWith("Reaction ", StringComparison.Ordinal)),
            diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error));
        return new(declarations, summary);
    }

    static string Text(string application, int documentCount, Description before, Description? after, string[] added, string[] removed, string? proposalId)
    {
        var text = new StringBuilder();
        text.Append($"The board shows '{application}' from {documentCount} document(s): {Counts(before.Summary)}.");
        if (after is null)
        {
            return text.ToString();
        }

        return text
            .Append(proposalId is null ? " It shows the sketch over it" : $" It shows proposal {proposalId} over it")
            .Append($", which would have {Counts(after.Summary)}.")
            .Append(added.Length == 0 ? " Nothing drawn is added." : $" Added: {string.Join(", ", added)}.")
            .Append(removed.Length == 0 ? " Nothing drawn is removed." : $" Removed: {string.Join(", ", removed)}.")
            .ToString();
    }

    static string Counts(Summary summary) => $"{summary.Slices} slice(s), {summary.Events} event(s), {summary.Reactions} reaction(s), {summary.Errors} error(s)";

    sealed record Summary(int Slices, int Events, int Reactions, int Errors);

    sealed record Description(HashSet<string> Declarations, Summary Summary);
}
