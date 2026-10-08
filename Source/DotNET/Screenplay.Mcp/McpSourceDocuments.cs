// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

static class McpSourceDocuments
{
    internal static ImmutableArray<WorkspaceOperation> Read(JsonElement arguments) =>
        [.. McpAstOperations.Values(arguments, "documents").Select(ReadDocument)];

    // Layout expansion and source authoring share the parser boundary. Resolve placements and route
    // candidates against the complete post-operation source set, not each fragment in isolation.
    internal static (ImmutableArray<WorkspaceOperation> Documents, ImmutableArray<Diagnostic> Diagnostics) Parse(
        ScreenplayWorkspace workspace, ImmutableArray<WorkspaceOperation> operations, bool generatedLayout = false)
    {
        var candidates = workspace.Documents.ToDictionary(document => document.Id);
        var targeted = new HashSet<DocumentId>();
        foreach (var operation in operations)
        {
            if (operation is AddWorkspaceDocument add)
            {
                var document = WorkspaceDocument.Create(add.StableKey, add.Path, add.Bytes.AsSpan());
                if (!targeted.Add(document.Id) || !candidates.TryAdd(document.Id, document))
                {
                    throw new McpFailure("A document creation duplicates an existing or targeted document identity.", -32602);
                }
                continue;
            }

            var id = operation switch
            {
                ReplaceWorkspaceDocument replace => replace.Document,
                MoveWorkspaceDocument move => move.Document,
                RenameWorkspaceDocument rename => rename.Document,
                RemoveWorkspaceDocument remove => remove.Document,
                _ => throw new McpFailure("Unsupported source document operation.", -32602)
            };
            if (!targeted.Add(id) || !candidates.TryGetValue(id, out var original))
            {
                throw new McpFailure("A document operation requires an existing document not targeted by another operation.", -32602);
            }

            switch (operation)
            {
                case ReplaceWorkspaceDocument replace:
                    candidates[id] = WorkspaceDocument.Create(id, original.StableKey, original.Path, replace.Bytes.AsSpan());
                    break;
                case MoveWorkspaceDocument move:
                    candidates[id] = WorkspaceDocument.Create(id, original.StableKey, move.Path, original.Bytes.AsSpan());
                    break;
                case RenameWorkspaceDocument rename:
                    candidates[id] = WorkspaceDocument.Create(id, rename.StableKey, original.Path, original.Bytes.AsSpan());
                    break;
                case RemoveWorkspaceDocument:
                    candidates.Remove(id);
                    break;
            }
        }

        McpRoot.CheckDocuments([.. candidates.Values], allowEmpty: true);
        if (candidates.Values.Select(document => document.Path.Value).Distinct(StringComparer.OrdinalIgnoreCase).Count() != candidates.Count)
        {
            throw new McpFailure("The final documents have colliding portable paths.", -32602);
        }

        var sources = candidates.Values.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal);
        var (placed, placementDiagnostics) = PlayImports.Resolve(sources.Keys, new InMemoryPlayDocumentSource(sources));
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        diagnostics.AddRange(placementDiagnostics);
        if (generatedLayout && placementDiagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
        {
            throw new McpFailure("Generated layout imports could not be resolved.");
        }

        if (placed.Any(document => !document.IsPlacementResolved))
        {
            return ([], diagnostics.ToImmutable());
        }

        var parser = (ICommandStreamCandidateParser)new ScreenplayCompiler();
        var streamCandidates = parser.CaptureCandidates(placed.Select(document => (SourceLineSplitter.Split(document.Source, path: document.Path), document.Placement)));
        var placements = placed.ToDictionary(document => document.Path, document => document.Placement, StringComparer.Ordinal);
        var documents = ImmutableArray.CreateBuilder<WorkspaceOperation>();
        foreach (var operation in operations)
        {
            var document = operation switch
            {
                AddWorkspaceDocument add => candidates[WorkspaceDocument.Create(add.StableKey, add.Path, add.Bytes.AsSpan()).Id],
                ReplaceWorkspaceDocument replace => candidates[replace.Document],
                _ => null
            };
            if (document is null)
            {
                documents.Add(operation);
                continue;
            }

            var parsed = parser.ParseWithCandidates(document.Text, document.Path.Value, placements[document.Path.Value], streamCandidates);
            diagnostics.AddRange(parsed.Diagnostics);
            if (generatedLayout && !parsed.Success)
            {
                throw new McpFailure($"Generated layout document '{document.Path}' could not be parsed.");
            }

            if (parsed.Success && parsed.Value is ApplicationSyntax syntax)
            {
                documents.Add(operation is AddWorkspaceDocument
                    ? new CreateWorkspaceSyntaxDocument(document.StableKey, document.Path, syntax, document.Encoding)
                    : new ReplaceWorkspaceSyntaxDocument(document.Id, syntax));
            }
        }

        return (documents.ToImmutable(), diagnostics.ToImmutable());
    }

    static WorkspaceOperation ReadDocument(JsonElement value)
    {
        var operation = McpJson.RequiredString(value, "operation");
        if (operation is not ("create-document" or "replace-document"))
        {
            return McpAstOperations.Document(value);
        }

        var fields = operation == "create-document"
            ? new[] { "operation", "stableKey", "path", "source", "encoding" }
            : ["operation", "documentId", "source"];
        McpJson.ValidateObject(value, fields, ["operation", "source"]);
        var source = McpJson.RequiredString(value, "source");
        var bytes = new UTF8Encoding(false, true).GetBytes(source);
        if (operation == "replace-document")
        {
            return new ReplaceWorkspaceDocument { Document = DocumentId.Parse(McpJson.RequiredString(value, "documentId")), Bytes = [.. bytes] };
        }

        var encoding = McpJson.Enumeration(value, "encoding", WorkspaceTextEncoding.Utf8);
        return new AddWorkspaceDocument
        {
            StableKey = McpJson.RequiredString(value, "stableKey"),
            Path = PortablePlayPath.Parse(McpJson.RequiredString(value, "path")),
            Bytes = encoding == WorkspaceTextEncoding.Utf8WithBom ? [.. Encoding.UTF8.Preamble, .. bytes] : [.. bytes]
        };
    }
}
