// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Nodes;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Defines the bounded changes <see cref="WorkspaceAuthoringFormatting.PreserveTrivia"/> can patch in place.
/// </summary>
enum WorkspaceTriviaChangeKind
{
    /// <summary>
    /// One identifier-valued member changes, patched through its proven identifier span.
    /// </summary>
    Identifier = 0,

    /// <summary>
    /// One literal's value changes, patched through the literal's parser-owned raw span.
    /// </summary>
    LiteralValue = 1,

    /// <summary>
    /// One property mapping's source expression changes, patched through the mapping's parser-owned source span.
    /// </summary>
    MappingSource = 2,

    /// <summary>
    /// One property mapping changes its target property, patched from the mapping start to the end of its source.
    /// </summary>
    Mapping = 3
}

/// <summary>
/// Represents one bounded change between an original and an intended typed AST.
/// </summary>
/// <param name="Path">The JSON pointer of the changed member or node.</param>
/// <param name="Kind">The <see cref="WorkspaceTriviaChangeKind"/>.</param>
/// <param name="Before">The original identifier text, for <see cref="WorkspaceTriviaChangeKind.Identifier"/> changes.</param>
/// <param name="After">The intended identifier text, for <see cref="WorkspaceTriviaChangeKind.Identifier"/> changes.</param>
sealed record WorkspaceTriviaChange(string Path, WorkspaceTriviaChangeKind Kind, string? Before = null, string? After = null);

/// <summary>
/// Finds the bounded changes between an original and an intended typed AST, rejecting any other structural change.
/// </summary>
/// <param name="entries">The original occurrences, carrying parser-owned source spans.</param>
sealed class WorkspaceTriviaChanges(IEnumerable<WorkspaceSyntaxEntry> entries)
{
    readonly Dictionary<string, SyntaxNode> _spanned = entries
        .Where(entry => entry.Node is LiteralExpressionSyntax { RawLocation: not null, RawLength: not null } or
            PropertyMappingSyntax { SourceLocation: not null, SourceLength: not null })
        .ToDictionary(entry => entry.Handle.Path, entry => entry.Node, StringComparer.Ordinal);

    /// <summary>
    /// Finds every bounded change.
    /// </summary>
    /// <param name="before">The original typed JSON.</param>
    /// <param name="after">The intended typed JSON.</param>
    /// <returns>The changes, in document traversal order.</returns>
    /// <exception cref="InvalidWorkspaceAuthoring">A change is structural and cannot be patched in place.</exception>
    internal IReadOnlyList<WorkspaceTriviaChange> Find(JsonNode? before, JsonNode? after)
    {
        var changes = new List<WorkspaceTriviaChange>();
        Differences(before, after, string.Empty, changes);
        return changes;
    }

    static bool EqualExcept(JsonObject before, JsonObject after, string member) =>
        before.Count == after.Count && before.All(pair => after.ContainsKey(pair.Key) && (pair.Key == member || JsonNode.DeepEquals(pair.Value, after[pair.Key])));

    void Differences(JsonNode? before, JsonNode? after, string path, List<WorkspaceTriviaChange> changes)
    {
        if (JsonNode.DeepEquals(before, after))
        {
            return;
        }

        if (Atomic(before, after, path) is { } atomic)
        {
            changes.Add(atomic);
            return;
        }

        if (before is JsonObject oldObject && after is JsonObject newObject && oldObject.Count == newObject.Count && oldObject.All(pair => newObject.ContainsKey(pair.Key)))
        {
            foreach (var pair in oldObject)
            {
                Differences(pair.Value, newObject[pair.Key], $"{path}/{pair.Key}", changes);
            }

            return;
        }

        if (before is JsonArray oldArray && after is JsonArray newArray && oldArray.Count == newArray.Count)
        {
            for (var index = 0; index < oldArray.Count; index++)
            {
                Differences(oldArray[index], newArray[index], $"{path}/{index}", changes);
            }

            return;
        }

        if (before is JsonValue oldValue && after is JsonValue newValue && oldValue.TryGetValue<string>(out var oldText) && newValue.TryGetValue<string>(out var newText))
        {
            changes.Add(new(path, WorkspaceTriviaChangeKind.Identifier, oldText, newText));
            return;
        }

        throw new InvalidWorkspaceAuthoring($"PreserveTrivia supports bounded identifier, literal value and property mapping changes, not structural change at '{path}'. Choose explicit CanonicalizeTouchedDocuments.");
    }

    WorkspaceTriviaChange? Atomic(JsonNode? before, JsonNode? after, string path)
    {
        var separator = path.LastIndexOf('/');
        if (separator >= 0 && path[(separator + 1)..] == "value" && _spanned.GetValueOrDefault(path[..separator]) is LiteralExpressionSyntax)
        {
            return new(path[..separator], WorkspaceTriviaChangeKind.LiteralValue);
        }

        if (_spanned.GetValueOrDefault(path) is not PropertyMappingSyntax || before is not JsonObject oldMapping || after is not JsonObject newMapping ||
            oldMapping.Count != newMapping.Count ||
            !oldMapping.All(pair => newMapping.ContainsKey(pair.Key) && (string.Equals(pair.Key, "property", StringComparison.Ordinal) || string.Equals(pair.Key, "source", StringComparison.Ordinal) || JsonNode.DeepEquals(pair.Value, newMapping[pair.Key]))))
        {
            return null;
        }

        if (!JsonNode.DeepEquals(oldMapping["property"], newMapping["property"]))
        {
            return new(path, WorkspaceTriviaChangeKind.Mapping);
        }

        // Only the source differs: a literal whose value alone changes keeps its own narrower value patch.
        var literalValueOnly = _spanned.ContainsKey($"{path}/source") && oldMapping["source"] is JsonObject oldSource &&
            newMapping["source"] is JsonObject newSource && EqualExcept(oldSource, newSource, "value");
        return literalValueOnly ? null : new(path, WorkspaceTriviaChangeKind.MappingSource);
    }
}
