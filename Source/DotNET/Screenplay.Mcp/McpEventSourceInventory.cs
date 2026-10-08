// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Logical authoring keys select exact owners. Source handles identify physical occurrences;
// neither enrolls an executable semantic or requirement identity.
sealed class McpEventSourceInventory
{
    readonly ScreenplayWorkspace _workspace;
    readonly Dictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _entries;

    internal McpEventSourceInventory(ScreenplayWorkspace workspace, WorkspacePhysicalReadView view)
    {
        _workspace = workspace;
        View = view;
        _entries = view.Entries.ToDictionary(entry => entry.Handle);
        Entries = [.. view.Entries.Where(entry => entry.Node is EventSourceSyntax or EventStreamSyntax)];
    }

    internal WorkspaceSyntaxEntry[] Entries { get; }

    internal WorkspacePhysicalReadView View { get; }

    internal string? Key(WorkspaceSyntaxEntry entry) => View.HasResolvedPlacement(entry) && Name(entry).Length > 0 ? JsonSerializer.Serialize(new
    {
        application = _workspace.IdentityCatalog.Application.ToString(), kind = Kind(entry), scope = Scope(entry), name = Name(entry)
    }) : null;

    internal EventSourceReadResolution Confidence(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax source
        ? View.Confidence.Resolve(source.Name)
        : View.Confidence.Resolve(Scope(entry).FirstOrDefault() ?? string.Empty, ((EventStreamSyntax)entry.Node).Name);

    internal bool AmbiguousOwner(WorkspaceSyntaxEntry entry) => Confidence(entry).State == "ambiguous";

    internal object Summary(WorkspaceSyntaxEntry entry) => new
    {
        authoringKey = Key(entry), keyKind = "logical-authoring-only", kind = Kind(entry), name = Name(entry), scope = Scope(entry),
        handle = McpAstHandles.Describe(entry.Handle), entry.Location, ownership = Ownership(entry), confidenceReasons = Confidence(entry).Reasons,
        placementResolved = View.HasResolvedPlacement(entry), inventoryComplete = View.IsComplete, readOnly = true,
        identifier = (entry.Node as EventSourceSyntax)?.Identifier, streamId = (entry.Node as EventStreamSyntax)?.StreamId,
        streamIdParts = (entry.Node as EventStreamSyntax)?.StreamIdParts.Select(part => new { part.Name, part.Type }),
        id = Inline(Pin(entry)),
        description = Inline(Description(entry)),
        metadata = Metadata(entry),
        syntaxOnly = true, executionAvailable = false, executionReadiness = "Not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302). Pins are rename-only authored metadata, not semantic identities."
    };

    internal IEnumerable<object> Details(WorkspaceSyntaxEntry entry)
    {
        yield return new
        {
            kind = "declaration", detailShape = "compact-header-v1", declaration = Summary(entry),
            streamCount = entry.Node is EventSourceSyntax source ? source.Streams.Count() : 0,
            description = Inline(Description(entry)),
            fullSyntax = new { tool = "read-ast", expectedRevision = _workspace.Revision.ToString(), documentId = entry.Handle.Document.ToString(), path = entry.Handle.Path, includeContent = true },
            originalSource = new { tool = "read-document", path = entry.Location.Path, bytePaging = true, revisionContract = "expectedSourceRevision" }
        };
        foreach (var child in Entries.Where(child => child.Node is EventStreamSyntax && child.Parent == entry.Handle).OrderBy(child => child.Index))
        {
            yield return new { kind = "stream", declaration = Summary(child) };
        }
    }

    internal IEnumerable<object> Routes()
    {
        foreach (var entry in View.Entries.Where(entry => entry.Node is CommandSyntax))
        {
            var command = (CommandSyntax)entry.Node;
            if (command.Stream is null && !command.StreamCandidates.Any()) continue;
            yield return new
            {
                kind = "command-route", command = command.Name, scope = Scope(entry), handle = McpAstHandles.Describe(entry.Handle),
                authoredRoute = command.Stream, ambiguousStreamCandidates = command.StreamCandidates,
                placementResolved = View.HasResolvedPlacement(entry), inventoryComplete = View.IsComplete,
                syntaxOnly = true, executionAvailable = false, executionReadiness = "Not admitted by any supported executable model (ESM) version yet (PLAY0268) (#302). Authored routing does not infer identity destinations."
            };
        }
    }

    static string? Pin(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax source ? source.Id : ((EventStreamSyntax)entry.Node).Id;
    static string? Description(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax source ? source.Description : ((EventStreamSyntax)entry.Node).Description;
    static string? Inline(string? value) => value is not null && Encoding.UTF8.GetByteCount(value) > 4096 ? null : value;

    static object Metadata(WorkspaceSyntaxEntry entry) => new
    {
        idBytes = Pin(entry) is { } id ? Encoding.UTF8.GetByteCount(id) : 0,
        descriptionBytes = Description(entry) is { } description ? Encoding.UTF8.GetByteCount(description) : 0,
        idInline = Inline(Pin(entry)) is not null,
        descriptionInline = Inline(Description(entry)) is not null,
        omittedValues = "Values over 4096 UTF-8 bytes are not inlined. Read the exact original document with read-document byte pages, or explicitly request full AST content."
    };

    static string Kind(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax ? "EventSource" : "EventStream";
    static string Name(WorkspaceSyntaxEntry entry) => entry.Node is EventSourceSyntax source ? source.Name : ((EventStreamSyntax)entry.Node).Name;

    string Ownership(WorkspaceSyntaxEntry entry)
    {
        return Confidence(entry).State;
    }

    string[] Scope(WorkspaceSyntaxEntry entry)
    {
        var names = new List<string>();
        for (var parent = entry.Parent; parent is not null && _entries.TryGetValue(parent, out var owner); parent = owner.Parent)
        {
            switch (owner.Node)
            {
                case EventSourceSyntax source: names.Add(source.Name); break;
                case ModuleSyntax module: names.Add(module.Name); break;
                case FeatureSyntax feature: names.Add(feature.Name); break;
                case SliceSyntax slice: names.Add(slice.Name); break;
            }
        }
        names.Reverse();

        return [.. names];
    }
}
