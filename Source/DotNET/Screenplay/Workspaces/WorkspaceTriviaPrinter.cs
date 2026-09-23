// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Printing;
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

        var entries = WorkspaceSyntaxIndex.ForSyntax(parsed.Value, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("trivia")));
        var changes = new WorkspaceTriviaChanges(entries).Find(WorkspaceSyntaxMutation.Json(parsed.Value), WorkspaceSyntaxMutation.Json(intended));
        var originals = entries.ToDictionary(entry => entry.Handle.Path, entry => entry.Node, StringComparer.Ordinal);
        var intendedNodes = changes.Any(change => change.Kind != WorkspaceTriviaChangeKind.Identifier)
            ? WorkspaceSyntaxIndex.ForSyntax(intended, SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("trivia"))).ToDictionary(entry => entry.Handle.Path, entry => entry.Node, StringComparer.Ordinal)
            : [];
        var tokens = WorkspaceSourceTokenizer.Tokenize(original).Tokens;
        var patches = changes.Select(change => change.Kind == WorkspaceTriviaChangeKind.Identifier
            ? IdentifierPatch(original, entries, tokens, change)
            : SpanPatch(original, tokens, change, originals[change.Path], intendedNodes.GetValueOrDefault(change.Path))).ToList();

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
            throw new InvalidWorkspaceAuthoring($"Trivia-preserving patches in '{original.Path}' did not reparse to the intended AST. Use explicit CanonicalizeTouchedDocuments or coordinated typed edits.");
        }

        return candidate;
    }

    static (int Offset, int Length, byte[] Bytes) IdentifierPatch(
        WorkspaceDocument original,
        ImmutableArray<WorkspaceSyntaxEntry> entries,
        ImmutableArray<WorkspaceSourceToken> tokens,
        WorkspaceTriviaChange change)
    {
        var owner = entries.Where(entry => change.Path.StartsWith($"{entry.Handle.Path}/", StringComparison.Ordinal))
            .MaxBy(entry => entry.Handle.Path.Length)!;
        var member = change.Path[(owner.Handle.Path.Length + 1)..];
        var token = tokens.SingleOrDefault(candidate => candidate.Kind == WorkspaceSourceTokenKind.Text && candidate.Span.Line == owner.Location.Line);
        if (token is null || !WorkspaceIdentifierSpans.Supports(owner.Node, member, token.Text))
        {
            throw Unsupported(original, change.Path);
        }

        var spans = WorkspaceIdentifierSpans.Find(token.Text, change.Before!).ToArray();
        if (spans.Length != 1)
        {
            throw Unsupported(original, change.Path);
        }

        var offset = token.Span.ByteOffset + Encoding.UTF8.GetByteCount(token.Text.AsSpan(0, spans[0].Offset));
        return (offset, Encoding.UTF8.GetByteCount(change.Before!), Encoding.UTF8.GetBytes(change.After!));
    }

    static (int Offset, int Length, byte[] Bytes) SpanPatch(
        WorkspaceDocument original,
        ImmutableArray<WorkspaceSourceToken> tokens,
        WorkspaceTriviaChange change,
        SyntaxNode before,
        SyntaxNode? after)
    {
        var (start, length, text) = (change.Kind, before, after) switch
        {
            (WorkspaceTriviaChangeKind.LiteralValue, LiteralExpressionSyntax literal, LiteralExpressionSyntax replacement) =>
                (literal.RawLocation!, literal.RawLength!.Value, ScreenplaySyntaxText.Expression(replacement)),
            (WorkspaceTriviaChangeKind.MappingSource, PropertyMappingSyntax mapping, PropertyMappingSyntax replacement) =>
                (mapping.SourceLocation!, mapping.SourceLength!.Value, ScreenplaySyntaxText.Expression(replacement.Source)),
            (WorkspaceTriviaChangeKind.Mapping, PropertyMappingSyntax mapping, PropertyMappingSyntax replacement) when mapping.SourceLocation!.Line == mapping.Location.Line =>
                (mapping.Location, mapping.SourceLocation.Column - mapping.Location.Column + mapping.SourceLength!.Value, $"{replacement.Property} = {ScreenplaySyntaxText.Expression(replacement.Source)}"),
            _ => throw Unsupported(original, change.Path)
        };
        var range = WorkspaceSourceRanges.Bytes(tokens, start, length) ?? throw Unsupported(original, change.Path);
        return (range.Offset, range.Length, Encoding.UTF8.GetBytes(text));
    }

    static InvalidWorkspaceAuthoring Unsupported(WorkspaceDocument document, string path) =>
        new($"No unique supported identifier, literal or mapping span for '{document.Path}:{path}'. Choose explicit CanonicalizeTouchedDocuments; no trivia was discarded.");
}
