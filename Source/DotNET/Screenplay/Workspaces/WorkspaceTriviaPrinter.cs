// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
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
        var tokensByLine = WorkspaceSourceTokenizer.Tokenize(original).Tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Text).ToLookup(token => token.Span.Line);

        // An inline event's declaration and production share one authored identifier.
        // Coalesce only byte-identical edits; conflicting or partial overlaps still fail closed.
        var patches = changes.Select(change => change.Kind == WorkspaceTriviaChangeKind.Identifier
            ? IdentifierPatch(original, originals, tokensByLine, change)
            : SpanPatch(original, tokensByLine, change, originals[change.Path], intendedNodes.GetValueOrDefault(change.Path)))
            .DistinctBy(patch => (patch.Offset, patch.Length, Convert.ToHexString(patch.Bytes)))
            .OrderByDescending(patch => patch.Offset).ToArray();
        var previousStart = original.Bytes.Length;
        foreach (var patch in patches)
        {
            if (patch.Offset + patch.Length > previousStart)
            {
                throw new InvalidWorkspaceAuthoring($"Overlapping identifier spans in '{original.Path}'. No source was changed.");
            }

            previousStart = patch.Offset;
        }

        // Copy each unchanged range once rather than shifting the document for every edit.
        using var bytes = new MemoryStream();
        var start = 0;
        for (var index = patches.Length - 1; index >= 0; index--)
        {
            var patch = patches[index];
            bytes.Write(original.Bytes.AsSpan(start, patch.Offset - start));
            bytes.Write(patch.Bytes);
            start = patch.Offset + patch.Length;
        }

        bytes.Write(original.Bytes.AsSpan(start..));
        var candidate = WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, bytes.ToArray());
        var reparsed = new ScreenplayCompiler().Parse(candidate.Text, candidate.Path.Value);
        if (!reparsed.Success || reparsed.Value is null || !SyntaxJson.StructurallyEqual(intended, reparsed.Value))
        {
            throw new InvalidWorkspaceAuthoring($"Trivia-preserving patches in '{original.Path}' did not reparse to the intended AST. Use explicit CanonicalizeTouchedDocuments or coordinated typed edits.");
        }

        return candidate;
    }

    static (int Offset, int Length, byte[] Bytes) IdentifierPatch(
        WorkspaceDocument original,
        Dictionary<string, SyntaxNode> originals,
        ILookup<int, WorkspaceSourceToken> tokensByLine,
        WorkspaceTriviaChange change)
    {
        var ownerPath = change.Path;
        SyntaxNode? owner = null;
        for (var separator = ownerPath.LastIndexOf('/'); separator >= 0; separator = ownerPath.LastIndexOf('/'))
        {
            ownerPath = ownerPath[..separator];
            if (originals.TryGetValue(ownerPath, out owner)) break;
        }

        if (owner is null) throw Unsupported(original, change.Path);
        var member = change.Path[(ownerPath.Length + 1)..];
        if (owner is ObjectMemberSyntax { RawLocation: { } keyStart, RawLength: { } keyLength } && member == "name")
        {
            var range = WorkspaceSourceRanges.Bytes([.. tokensByLine[keyStart.Line]], keyStart, keyLength) ?? throw Unsupported(original, change.Path);
            var authored = Encoding.UTF8.GetString(original.Bytes.AsSpan(range.Offset, range.Length));
            if (JsonSerializer.Deserialize<string>(authored) != change.Before)
            {
                throw Unsupported(original, change.Path);
            }

            return (range.Offset, range.Length, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(change.After)));
        }

        var token = tokensByLine[owner.Location.Line].SingleOrDefault();
        if (token is null || !WorkspaceIdentifierSpans.Supports(owner, member, token.Text))
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
        ILookup<int, WorkspaceSourceToken> tokensByLine,
        WorkspaceTriviaChange change,
        SyntaxNode before,
        SyntaxNode? after)
    {
        if (change.Kind == WorkspaceTriviaChangeKind.EventPin && before is EventSyntax declaration && after is EventSyntax changed)
        {
            return EventPinPatch(original, declaration, changed);
        }

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
        var range = WorkspaceSourceRanges.Bytes([.. tokensByLine[start.Line]], start, length) ?? throw Unsupported(original, change.Path);
        return (range.Offset, range.Length, Encoding.UTF8.GetBytes(text));
    }

    static (int Offset, int Length, byte[] Bytes) EventPinPatch(WorkspaceDocument original, EventSyntax before, EventSyntax after)
    {
        var tokens = WorkspaceSourceTokenizer.Tokenize(original).Tokens;
        if (before.Id is not null && after.Id is null && before.DirectiveLocations.TryGetValue("id", out var location))
        {
            var line = tokens.Where(value => value.Span.Line == location.Line).ToArray();
            if (line.Any(value => value.Kind == WorkspaceSourceTokenKind.Comment))
            {
                // Keep a trailing comment on its original line, exactly once.
                var token = line.Single(value => value.Kind == WorkspaceSourceTokenKind.Text);
                return (token.Span.ByteOffset, token.Span.ByteLength, []);
            }

            var start = line[0].Span.ByteOffset;
            var end = line[^1].Span;
            return (start, end.ByteOffset + end.ByteLength - start, []);
        }

        if (before.Id is not null || after.Id is null)
        {
            throw Unsupported(original, "event/id");
        }

        var header = tokens.Where(value => value.Span.Line == before.Location.Line).ToArray();
        var ending = header.LastOrDefault(value => value.Kind == WorkspaceSourceTokenKind.LineEnding);
        var indentation = header.FirstOrDefault(value => value.Kind == WorkspaceSourceTokenKind.Indentation)?.Text ?? string.Empty;
        var next = tokens.FirstOrDefault(value => value.Span.Line > before.Location.Line && value.Kind == WorkspaceSourceTokenKind.Text);
        var childIndentation = next is not null && next.Span.Column > indentation.Length + 1
            ? tokens.FirstOrDefault(value => value.Span.Line == next.Span.Line && value.Kind == WorkspaceSourceTokenKind.Indentation)?.Text ?? (indentation + "  ")
            : indentation + "  ";
        var newline = ending?.Text ?? "\n";
        var text = (ending is null ? newline : string.Empty) + childIndentation + "id " + JsonSerializer.Serialize(after.Id) + newline;
        return (ending is null ? original.Bytes.Length : ending.Span.ByteOffset + ending.Span.ByteLength, 0, Encoding.UTF8.GetBytes(text));
    }

    static InvalidWorkspaceAuthoring Unsupported(WorkspaceDocument document, string path) =>
        new($"No unique supported identifier, literal or mapping span for '{document.Path}:{path}'. Choose explicit CanonicalizeTouchedDocuments; no trivia was discarded.");
}
