// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Represents one source occurrence. Multiple module/feature occurrences can share one logical address.
/// Unsupported semantic kinds deliberately have no address or stable semantic identity.
/// </summary>
public sealed record WorkspaceSyntaxEntry
{
    /// <summary>
    /// Gets the revision-bound occurrence handle.
    /// </summary>
    public required WorkspaceNodeHandle Handle { get; init; }

    /// <summary>
    /// Gets the parent occurrence, or null for a document root.
    /// </summary>
    public WorkspaceNodeHandle? Parent { get; init; }

    /// <summary>
    /// Gets the camel-case parent member, or null for a document root.
    /// </summary>
    public string? Member { get; init; }

    /// <summary>
    /// Gets the original collection index, or null for a singular child or document root.
    /// </summary>
    public int? Index { get; init; }

    /// <summary>
    /// Gets the concrete CLR syntax kind.
    /// </summary>
    public string Kind => Node.GetType().Name;

    /// <summary>
    /// Gets the original typed syntax, suitable for structural expectations and replacements.
    /// </summary>
    public required SyntaxNode Node { get; init; }

    /// <summary>
    /// Gets the original source location.
    /// </summary>
    public SourceLocation Location => Node.Location;

    /// <summary>
    /// Gets the semantic address when this occurrence occupies a supported declaration slot.
    /// </summary>
    public SemanticAddress? Address { get; init; }

    /// <summary>
    /// Gets an existing catalog assignment, never an invented identity for an unsupported construct.
    /// </summary>
    public SemanticId? SemanticId { get; init; }

    /// <summary>
    /// Gets an existing event contract assignment, when present.
    /// </summary>
    public EventContractId? EventContractId { get; init; }
}

/// <summary>
/// Indexes every original typed syntax occurrence, including each document root.
/// Handles are snapshot-local and must not be persisted as semantic identities.
/// </summary>
public sealed class WorkspaceSyntaxIndex
{
    // Retain the original typed-property traversal order, but let the wire descriptors
    // decide membership and names (including the reserved kind/syntaxKind distinction).
    static readonly Dictionary<Type, SyntaxMember[]> _children = SyntaxKinds.All.ToDictionary(
        descriptor => descriptor.Type,
        descriptor => descriptor.Type.GetProperties()
            .Join(descriptor.Members, property => property.Name, member => member.Property.Name, (_, member) => member)
            .Where(member => typeof(SyntaxNode).IsAssignableFrom(member.Type) ||
                (member.ElementType is not null && typeof(SyntaxNode).IsAssignableFrom(member.ElementType)))
            .ToArray());

    readonly IReadOnlyDictionary<WorkspaceNodeHandle, WorkspaceSyntaxEntry> _handles;

    WorkspaceSyntaxIndex(ImmutableArray<WorkspaceSyntaxEntry> entries, ImmutableArray<Diagnostic> diagnostics)
    {
        Entries = entries;
        Diagnostics = diagnostics;
        _handles = entries.ToDictionary(entry => entry.Handle);
    }

    /// <summary>
    /// Gets original occurrences in stable document and typed member traversal order.
    /// </summary>
    public ImmutableArray<WorkspaceSyntaxEntry> Entries { get; }

    /// <summary>
    /// Gets parse diagnostics; erroneous documents are not indexed as editable syntax.
    /// </summary>
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    /// <summary>
    /// Creates an index from exact source without requiring ESM binding.
    /// </summary>
    /// <param name="workspace">The original workspace.</param>
    /// <returns>The occurrence index and parser diagnostics.</returns>
    public static WorkspaceSyntaxIndex Create(ScreenplayWorkspace workspace)
    {
        var entries = ImmutableArray.CreateBuilder<WorkspaceSyntaxEntry>();
        var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
        var semantics = workspace.IdentityCatalog.Semantics.ToDictionary(assignment => assignment.Address, assignment => assignment.Id);
        var events = workspace.IdentityCatalog.EventContracts.ToDictionary(assignment => assignment.Address, assignment => assignment.Id);
        foreach (var document in workspace.Documents)
        {
            var parsed = new ScreenplayCompiler().Parse(document.Text, document.Path.Value);
            diagnostics.AddRange(parsed.Diagnostics);
            if (parsed.Success && parsed.Value is not null)
            {
                Visit(parsed.Value, new(workspace.Revision, document.Id, string.Empty), null, null, null, [], workspace.IdentityCatalog.Application, semantics, events, entries);
            }
        }

        return new(entries.ToImmutable(), diagnostics.ToImmutable());
    }

    /// <summary>
    /// Finds an exact original occurrence, including its revision and document identity.
    /// </summary>
    /// <param name="handle">The snapshot handle.</param>
    /// <returns>The entry, or null for an unknown or stale handle.</returns>
    public WorkspaceSyntaxEntry? Find(WorkspaceNodeHandle handle) => _handles.GetValueOrDefault(handle);

    internal static ImmutableArray<WorkspaceSyntaxEntry> ForSyntax(ApplicationSyntax syntax, SemanticIdentityCatalog catalog)
    {
        var entries = ImmutableArray.CreateBuilder<WorkspaceSyntaxEntry>();
        Visit(
            syntax,
            new(default, default, string.Empty),
            null,
            null,
            null,
            [],
            catalog.Application,
            catalog.Semantics.ToDictionary(assignment => assignment.Address, assignment => assignment.Id),
            catalog.EventContracts.ToDictionary(assignment => assignment.Address, assignment => assignment.Id),
            entries);
        return entries.ToImmutable();
    }

    static void Visit(
        SyntaxNode node,
        WorkspaceNodeHandle handle,
        WorkspaceSyntaxEntry? parent,
        string? member,
        int? index,
        ImmutableArray<WorkspaceSyntaxEntry> ancestors,
        ApplicationIdentity application,
        IReadOnlyDictionary<SemanticAddress, SemanticId> semantics,
        IReadOnlyDictionary<SemanticAddress, EventContractId> events,
        ImmutableArray<WorkspaceSyntaxEntry>.Builder entries)
    {
        var address = WorkspaceSyntaxAddresses.Address(node, member, ancestors, application);
        var entry = new WorkspaceSyntaxEntry
        {
            Handle = handle,
            Parent = parent?.Handle,
            Member = member,
            Index = index,
            Node = node,
            Address = address,
            SemanticId = address is not null && semantics.TryGetValue(address, out var semanticId) ? semanticId : null,
            EventContractId = address is not null && events.TryGetValue(address, out var eventId) ? eventId : null
        };
        entries.Add(entry);
        var lineage = ancestors.Add(entry);
        foreach (var childMember in _children[node.GetType()])
        {
            var name = childMember.Name;
            var value = childMember.Property.GetValue(node);
            var path = $"{handle.Path}/{name}";
            if (value is SyntaxNode child)
            {
                Visit(child, handle with { Path = path }, entry, name, null, lineage, application, semantics, events, entries);
            }
            else if (value is IEnumerable children and not string)
            {
                var childIndex = 0;
                foreach (var item in children)
                {
                    if (item is SyntaxNode childNode)
                    {
                        Visit(childNode, handle with { Path = $"{path}/{childIndex}" }, entry, name, childIndex, lineage, application, semantics, events, entries);
                    }

                    childIndex++;
                }
            }
        }
    }
}
