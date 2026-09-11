// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class ProducedEventMappingSourcePatch
{
    internal static WorkspaceConflict? Apply(
        UpdateProducedEventMappingSource operation,
        ScreenplayWorkspace workspace,
        Dictionary<DocumentId, WorkspaceDocument> candidates,
        SemanticCommand command,
        SemanticEventContract producedEvent,
        SemanticProperty target,
        SemanticProperty oldSource,
        SemanticProperty newSource)
    {
        var owners = workspace.Compilation.Value!.SourceMap.Entries
            .Where(entry => entry.SemanticId == operation.Command && entry.Role == SemanticSourceMapRole.Declaration).ToArray();
        if (owners.Length != 1 || !candidates.TryGetValue(owners[0].Span.Document, out var document))
        {
            return Unsupported("The command must have exactly one source owner.");
        }

        var parsed = new ScreenplayCompiler().Parse(document.Text, document.Path.Value);
        if (!parsed.Success)
        {
            return Unsupported("The owning document cannot be parsed independently.");
        }

        var owner = owners[0].Span;
        var commands = Commands(parsed.Value!.Modules.SelectMany(module => module.Features))
            .Where(value => value.Name == command.Name && value.Location.Line == owner.StartLine && value.Location.Column == owner.StartColumn).ToArray();
        if (commands.Length != 1)
        {
            return Unsupported("The semantic command source owner cannot be matched to one parser declaration.");
        }

        var productions = commands[0].Produces.Where(value => value.Event == producedEvent.Name).ToArray();
        if (productions.Length != 1 || productions[0].When is not null)
        {
            return Unsupported("The parser must identify exactly one unconditional production.");
        }

        var mappings = productions[0].Mappings.Where(value => value.Property == target.Name).ToArray();
        if (mappings.Length != 1 || mappings[0].Source is not PathExpressionSyntax path || path.Path != oldSource.Name ||
            mappings[0].SourceLocation is not { } location || mappings[0].SourceLength is not { } length)
        {
            return Unsupported("The mapping must be explicit with an exact parser-owned direct-property source range.");
        }

        var tokens = WorkspaceSourceTokenizer.Tokenize(document).Tokens;
        var tokensAtSource = tokens.Where(token => token.Kind == WorkspaceSourceTokenKind.Text && token.Span.Line == location.Line &&
            token.Span.Column <= location.Column && location.Column - token.Span.Column + length <= token.Span.TextLength).ToArray();
        if (tokensAtSource.Length != 1 || length != oldSource.Name.Length)
        {
            return Unsupported("The parser-owned source range does not identify one exact workspace token.");
        }

        var token = tokensAtSource[0];
        var offset = token.Span.TextOffset + location.Column - token.Span.Column;
        if (!document.Text.AsSpan(offset, length).SequenceEqual(oldSource.Name.AsSpan()))
        {
            return Unsupported("The parser-owned source range disagrees with the authored command property.");
        }

        var byteOffset = token.Span.ByteOffset + Encoding.UTF8.GetByteCount(document.Text.AsSpan(token.Span.TextOffset, offset - token.Span.TextOffset));
        var byteLength = Encoding.UTF8.GetByteCount(document.Text.AsSpan(offset, length));
        var replacement = Encoding.UTF8.GetBytes(newSource.Name);
        var bytes = ImmutableArray.CreateBuilder<byte>(document.Bytes.Length - byteLength + replacement.Length);
        bytes.AddRange(document.Bytes.AsSpan(0, byteOffset));
        bytes.AddRange(replacement);
        bytes.AddRange(document.Bytes.AsSpan()[(byteOffset + byteLength)..]);
        var result = bytes.ToImmutable();
        if (!document.Bytes.AsSpan().SequenceEqual(result.AsSpan()))
        {
            candidates[document.Id] = WorkspaceDocument.Create(document.Id, document.StableKey, document.Path, result.AsSpan());
        }

        return null;
    }

    internal static WorkspaceConflict? VerifyCanonical(ScreenplayWorkspace candidate)
    {
        var documents = ImmutableArray.CreateBuilder<SemanticSourceDocument>();
        foreach (var document in candidate.Documents)
        {
            var syntax = new ScreenplayCompiler().Parse(document.Text, document.Path.Value);
            if (!syntax.Success)
            {
                return Unsupported("The candidate cannot be parsed for canonical equivalence verification.");
            }

            var printed = new ScreenplayPrinter().Print(syntax.Value!);
            documents.Add(SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, printed));
        }

        // Printing is proof only. The candidate retains the authored bytes and the original document partition.
        var canonical = new SemanticModelCompiler().Compile(
            candidate.ApplicationName,
            SemanticDocumentSet.Create(documents.ToImmutable(), candidate.IdentityCatalog));
        if (!canonical.Success || !ProducedEventMappingPatch.Equivalent(candidate.Compilation.Value!.Model, canonical.Value!.Model))
        {
            return Unsupported("Canonical print/recompile does not preserve the complete candidate semantics.");
        }

        return null;
    }

    static IEnumerable<CommandSyntax> Commands(IEnumerable<FeatureSyntax> features) =>
        features.SelectMany(feature => feature.Slices.SelectMany(slice => slice.Commands).Concat(Commands(feature.Features)));

    static WorkspaceConflict Unsupported(string message) => ProducedEventMappingPatch.Conflict(WorkspaceConflictKind.UnsupportedSemanticField, message);
}
