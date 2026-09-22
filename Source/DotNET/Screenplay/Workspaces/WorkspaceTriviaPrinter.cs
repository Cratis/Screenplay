// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceTriviaPrinter
{
    internal static WorkspaceDocument Print(WorkspaceDocument original, ApplicationSyntax intended)
    {
        var parsed = new ScreenplayCompiler().Parse(original.Text, original.Path.Value);
        if (!parsed.Success || parsed.Value is null)
        {
            throw new InvalidWorkspaceAuthoring($"Cannot preserve trivia in unparseable document '{original.Path}'.");
        }

        var changes = new List<(string Path, string Before, string After)>();
        Differences(WorkspaceSyntaxMutation.Json(parsed.Value), WorkspaceSyntaxMutation.Json(intended), string.Empty, changes);
        var entries = WorkspaceSyntaxIndex.ForSyntax(parsed.Value, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("trivia")));
        var tokens = WorkspaceSourceTokenizer.Tokenize(original).Tokens;
        var patches = new List<(int Offset, int Length, byte[] Bytes)>();
        foreach (var change in changes)
        {
            var owner = entries.Where(entry => change.Path.StartsWith($"{entry.Handle.Path}/", StringComparison.Ordinal))
                .MaxBy(entry => entry.Handle.Path.Length)!;
            var member = change.Path[(owner.Handle.Path.Length + 1)..];
            var token = tokens.SingleOrDefault(candidate => candidate.Kind == WorkspaceSourceTokenKind.Text && candidate.Span.Line == owner.Location.Line);
            if (token is null || !WorkspaceIdentifierSpans.Supports(owner.Node, member, token.Text))
            {
                throw Unsupported(original, change.Path);
            }

            var spans = WorkspaceIdentifierSpans.Find(token.Text, change.Before).ToArray();
            if (spans.Length != 1)
            {
                throw Unsupported(original, change.Path);
            }

            var offset = token.Span.ByteOffset + Encoding.UTF8.GetByteCount(token.Text.AsSpan(0, spans[0].Offset));
            patches.Add((offset, Encoding.UTF8.GetByteCount(change.Before), Encoding.UTF8.GetBytes(change.After)));
        }

        var bytes = original.Bytes.ToArray().ToList();
        var previousStart = bytes.Count;
        foreach (var patch in patches.OrderByDescending(patch => patch.Offset))
        {
            if (patch.Offset + patch.Length > previousStart)
            {
                throw new InvalidWorkspaceAuthoring($"Overlapping identifier spans in '{original.Path}'. No source was changed.");
            }

            bytes.RemoveRange(patch.Offset, patch.Length);
            bytes.InsertRange(patch.Offset, patch.Bytes);
            previousStart = patch.Offset;
        }

        var candidate = WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, [.. bytes]);
        var reparsed = new ScreenplayCompiler().Parse(candidate.Text, candidate.Path.Value);
        if (!reparsed.Success || reparsed.Value is null || !SyntaxJson.StructurallyEqual(intended, reparsed.Value))
        {
            throw new InvalidWorkspaceAuthoring($"Identifier patches in '{original.Path}' did not reparse to the intended AST. Use explicit CanonicalizeTouchedDocuments or coordinated typed edits.");
        }

        return candidate;
    }

    static void Differences(JsonNode? before, JsonNode? after, string path, List<(string Path, string Before, string After)> changes)
    {
        if (JsonNode.DeepEquals(before, after))
        {
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
            changes.Add((path, oldText, newText));
            return;
        }

        throw new InvalidWorkspaceAuthoring($"PreserveTrivia supports bounded identifier changes, not structural change at '{path}'. Choose explicit CanonicalizeTouchedDocuments.");
    }

    static InvalidWorkspaceAuthoring Unsupported(WorkspaceDocument document, string path) =>
        new($"No unique supported identifier span for '{document.Path}:{path}'. Choose explicit CanonicalizeTouchedDocuments; no trivia was discarded.");
}
