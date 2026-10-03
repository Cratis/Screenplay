// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

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

        // Resolve and parse the original snapshot once. Source-map positions and mapping spans must
        // refer to that exact document, never a standalone parse that discards its import placement.
        var index = WorkspaceSyntaxIndex.Create(workspace);
        if (index.UnresolvedPlacementDocuments.Any(value => value.Id == document.Id))
        {
            return Unsupported("UnresolvedPlacement: repair conflicting or cyclic imports before patching the owning document.");
        }

        var original = workspace.Documents.Single(value => value.Id == document.Id);
        if (!document.Bytes.AsSpan().SequenceEqual(original.Bytes.AsSpan()))
        {
            return Unsupported("The owning document no longer matches the original source snapshot.");
        }

        if (index.Find(new(workspace.Revision, document.Id, string.Empty))?.Node is not ApplicationSyntax)
        {
            return Unsupported("The owning document cannot be parsed at its resolved import placement.");
        }

        var owner = owners[0].Span;
        var commands = index.Entries.Where(entry => entry.Handle.Document == document.Id && entry.SemanticId == operation.Command &&
                entry.Node is CommandSyntax && entry.Location.Line == owner.StartLine && entry.Location.Column == owner.StartColumn)
            .Select(entry => (CommandSyntax)entry.Node).Where(value => value.Name == command.Name).ToArray();
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
        var range = WorkspaceSourceRanges.Bytes(tokens, location, length);
        if (range is not { } bytesAtSource || length != oldSource.Name.Length)
        {
            return Unsupported("The parser-owned source range does not identify one exact workspace token.");
        }

        var (byteOffset, byteLength) = bytesAtSource;
        if (!document.Bytes.AsSpan(byteOffset, byteLength).SequenceEqual(Encoding.UTF8.GetBytes(oldSource.Name)))
        {
            return Unsupported("The parser-owned source range disagrees with the authored command property.");
        }

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
        var index = WorkspaceSyntaxIndex.Create(candidate);
        if (!index.UnresolvedPlacementDocuments.IsEmpty)
        {
            return Unsupported("UnresolvedPlacement: repair conflicting or cyclic imports before canonical equivalence verification.");
        }

        var documents = ImmutableArray.CreateBuilder<SemanticSourceDocument>();
        foreach (var document in candidate.Documents)
        {
            if (index.Find(new(candidate.Revision, document.Id, string.Empty))?.Node is not ApplicationSyntax syntax)
            {
                return Unsupported("The candidate cannot be parsed for canonical equivalence verification.");
            }

            var printed = new ScreenplayPrinter().Print(syntax);
            var reparsed = new ScreenplayCompiler().Parse(printed, document.Path.Value, index.Placement(document));
            if (!reparsed.Success || reparsed.Value is null || !SyntaxJson.StructurallyEqual(syntax, reparsed.Value))
            {
                return Unsupported("Canonical printing does not preserve the document syntax at its resolved import placement.");
            }

            documents.Add(SemanticSourceDocument.Create(document.Id, document.StableKey, document.Path.Value, printed));
        }

        // Printing is proof only. The candidate retains the authored bytes and the original document partition.
        var canonical = new SemanticModelCompiler().Compile(
            candidate.ApplicationName,
            SemanticDocumentSet.Create(documents.ToImmutable(), candidate.IdentityCatalog, candidate.AttachmentContents));
        if (!canonical.Success || !ProducedEventMappingPatch.Equivalent(candidate.Compilation.Value!.Model, canonical.Value!.Model))
        {
            return Unsupported("Canonical print/recompile does not preserve the complete candidate semantics.");
        }

        return null;
    }

    static WorkspaceConflict Unsupported(string message) => ProducedEventMappingPatch.Conflict(WorkspaceConflictKind.UnsupportedSemanticField, message);
}
