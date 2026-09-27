// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspaceAstEdits(WorkspaceSyntaxIndex index)
{
    static readonly JsonSerializerOptions _jsonOptions = new() { MaxDepth = 256 };

    readonly Dictionary<DocumentId, JsonNode> _roots = index.Entries.Where(entry => entry.Parent is null)
        .ToDictionary(entry => entry.Handle.Document, entry => ToJson(entry.Node));
    readonly List<Edit> _edits = [];
    readonly Dictionary<JsonNode, SourceLocation> _sourceLocations = [];
    readonly Dictionary<JsonNode, ImmutableArray<SourceComment>> _sourceComments = [];
    readonly Dictionary<JsonNode, IReadOnlyDictionary<string, SourceLocation>> _directiveLocations = [];
    readonly Dictionary<JsonNode, AutoMapMode> _parsedAutoMapModes = [];

    internal ImmutableArray<DocumentId> Touched => [.. _edits.SelectMany(edit => new[] { edit.Target?.Handle.Document, edit.Destination?.Parent.Handle.Document }).OfType<DocumentId>().Distinct()];

    internal static bool Contains(WorkspaceNodeHandle ancestor, WorkspaceNodeHandle descendant) => ancestor.Document == descendant.Document &&
        (ancestor.Path == descendant.Path || descendant.Path.StartsWith($"{ancestor.Path}/", StringComparison.Ordinal));

    internal void Prepare(ImmutableArray<WorkspaceAstOperation> operations)
    {
        foreach (var operation in operations)
        {
            _edits.Add(operation switch
            {
                AddWorkspaceNode add => new(null, null, Destination(add.Parent, add.ExpectedParent, add.Member, add.Index, add.Node), add.Node),
                ReplaceWorkspaceNode replace => Replacement(replace),
                RemoveWorkspaceNode remove => Removal(remove.Target, remove.Expected),
                MoveWorkspaceNode move => Move(move),
                _ => throw new InvalidWorkspaceAuthoring("An AST operation is null or unsupported.")
            });
        }

        foreach (var edit in _edits.Where(edit => edit.Value is not null))
        {
            _ = SyntaxJson.Deserialize(SyntaxJson.Serialize(edit.Value!));
        }

        ValidateOverlap();
    }

    internal Dictionary<DocumentId, ApplicationSyntax> Apply()
    {
        foreach (var entry in index.Entries)
        {
            _sourceLocations[Resolve(entry.Handle)] = entry.Location;
            _sourceComments[Resolve(entry.Handle)] = entry.Node.SourceComments;
            _directiveLocations[Resolve(entry.Handle)] = entry.Node.DirectiveLocations;
            if (entry.Node.ParsedAutoMapMode is { } mode)
            {
                _parsedAutoMapModes[Resolve(entry.Handle)] = mode;
            }
        }

        foreach (var edit in _edits.Where(edit => edit.Target is not null))
        {
            Replace(edit.Target!, edit.Original!, edit.Destination is null && edit.Value is not null ? ToJson(edit.Value) : null, edit.Value);
        }

        foreach (var edit in _edits.Where(edit => edit.Destination is not null))
        {
            Insert(edit.Destination!, ToJson(edit.Value!));
        }

        return Touched.ToDictionary(
            document => document,
            document => RestoreSourceLocations(
                _roots[document],
                SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_roots[document], _jsonOptions)) as ApplicationSyntax
                    ?? throw new InvalidWorkspaceAuthoring("A document root must remain an ApplicationSyntax.")));
    }

    static bool RequireSlotType(WorkspaceSyntaxEntry parent, string member, SyntaxNode value)
    {
        var property = parent.Node.GetType().GetProperties().SingleOrDefault(candidate => JsonNamingPolicy.CamelCase.ConvertName(candidate.Name) == member);
        var type = property?.PropertyType;
        var collection = type?.IsGenericType == true && type.GetGenericTypeDefinition() == typeof(IEnumerable<>);
        var nodeType = collection ? type!.GetGenericArguments()[0] : type;
        if (value is null || nodeType is null || !typeof(SyntaxNode).IsAssignableFrom(nodeType) || !nodeType.IsInstanceOfType(value))
        {
            throw new InvalidWorkspaceAuthoring($"Member '{member}' does not accept '{value?.GetType().Name ?? "null"}'.");
        }

        return collection;
    }

    static void Insert(Insertion destination, JsonNode node)
    {
        if (!destination.Collection)
        {
            destination.Owner[destination.Member] = node;
            return;
        }

        var array = destination.Owner[destination.Member] as JsonArray;
        if (array is null)
        {
            array = [];
            destination.Owner[destination.Member] = array;
        }

        array.Insert(destination.Anchor is null ? array.Count : array.IndexOf(destination.Anchor), node);
    }

    static JsonNode ToJson(SyntaxNode node) => JsonNode.Parse(SyntaxJson.Serialize(node).GetRawText(), documentOptions: new JsonDocumentOptions { MaxDepth = 256 })!;

    Edit Replacement(ReplaceWorkspaceNode operation)
    {
        var entry = Expected(operation.Target, operation.Expected);
        if (entry.Parent is null)
        {
            if (operation.Node is not ApplicationSyntax)
            {
                throw new InvalidWorkspaceAuthoring("A document root must be an ApplicationSyntax.");
            }
        }
        else
        {
            RequireSlotType(index.Find(entry.Parent)!, entry.Member!, operation.Node);
        }

        return new(entry, Resolve(entry.Handle), null, operation.Node);
    }

    Edit Removal(WorkspaceNodeHandle target, SyntaxNode expected)
    {
        var entry = Expected(target, expected);
        if (entry.Parent is null)
        {
            throw new InvalidWorkspaceAuthoring("Remove a document root with RemoveWorkspaceDocument, not an AST removal.");
        }

        if (entry.Index is null)
        {
            var parent = index.Find(entry.Parent)!;
            var property = parent.Node.GetType().GetProperties().Single(candidate => JsonNamingPolicy.CamelCase.ConvertName(candidate.Name) == entry.Member);
            if (new NullabilityInfoContext().Create(property).ReadState != NullabilityState.Nullable)
            {
                throw new InvalidWorkspaceAuthoring($"Required singular child '{entry.Member}' cannot be removed or moved; replace its owner explicitly.");
            }
        }

        return new(entry, Resolve(target), null, null);
    }

    Edit Move(MoveWorkspaceNode operation)
    {
        var removal = Removal(operation.Target, operation.Expected);
        var target = removal.Target!;
        var destination = Destination(operation.Parent, operation.ExpectedParent, operation.Member, operation.Index, target.Node);
        return removal with { Destination = destination, Value = target.Node };
    }

    WorkspaceSyntaxEntry Expected(WorkspaceNodeHandle handle, SyntaxNode expected)
    {
        var entry = index.Find(handle) ?? throw new InvalidWorkspaceAuthoring("An AST handle is stale, unknown, or does not identify an original syntax occurrence.");
        if (expected is null || !SyntaxJson.StructurallyEqual(entry.Node, expected))
        {
            throw new InvalidWorkspaceAuthoring($"The expected AST value at '{handle.Path}' does not match the original snapshot.");
        }

        return entry;
    }

    Insertion Destination(WorkspaceNodeHandle parent, SyntaxNode expected, string member, int? position, SyntaxNode value)
    {
        var entry = Expected(parent, expected);
        var collection = RequireSlotType(entry, member, value);
        var owner = Resolve(parent).AsObject();
        if (!owner.ContainsKey(member))
        {
            throw new InvalidWorkspaceAuthoring($"Unknown typed syntax member '{member}'.");
        }

        if (collection)
        {
            var array = owner[member] as JsonArray;
            var count = array?.Count ?? 0;
            var boundary = position ?? count;
            if (boundary < 0 || boundary > count)
            {
                throw new InvalidWorkspaceAuthoring($"Insertion index '{boundary}' is outside the original '{member}' collection.");
            }

            return new(entry, owner, member, boundary, array is not null && boundary < count ? array[boundary] : null, true);
        }

        if (position is not null || owner[member] is not null)
        {
            throw new InvalidWorkspaceAuthoring($"Singular member '{member}' must be empty and cannot have an index; use replacement for an existing child.");
        }

        return new(entry, owner, member, null, null, false);
    }

    void ValidateOverlap()
    {
        var targets = _edits.Where(edit => edit.Target is not null).Select(edit => edit.Target!.Handle).ToArray();
        for (var first = 0; first < targets.Length; first++)
        {
            for (var second = first + 1; second < targets.Length; second++)
            {
                if (Contains(targets[first], targets[second]) || Contains(targets[second], targets[first]))
                {
                    throw new InvalidWorkspaceAuthoring("AST edits overlap an original node or its descendants.");
                }
            }
        }

        var destinations = _edits.Where(edit => edit.Destination is not null).Select(edit => edit.Destination!).ToArray();
        foreach (var destination in destinations)
        {
            if (targets.Any(target => Contains(target, destination.Parent.Handle)))
            {
                throw new InvalidWorkspaceAuthoring("An insertion parent is removed, replaced, or moved, including a move into itself.");
            }

            if (destination.Anchor is not null && _edits.Exists(edit => ReferenceEquals(edit.Original, destination.Anchor)))
            {
                throw new InvalidWorkspaceAuthoring("An original insertion anchor is also removed, replaced, or moved.");
            }
        }

        if (destinations.GroupBy(destination => (destination.Parent.Handle, destination.Member, destination.Boundary)).Any(group => group.Count() > 1))
        {
            throw new InvalidWorkspaceAuthoring("Multiple insertions claim the same original member and insertion boundary.");
        }
    }

    JsonNode Resolve(WorkspaceNodeHandle handle)
    {
        var current = _roots[handle.Document];
        foreach (var segment in handle.Path.Split('/').Skip(1))
        {
            current = current is JsonArray array ? array[int.Parse(segment, System.Globalization.CultureInfo.InvariantCulture)]! : current[segment]!;
        }

        return current;
    }

    void Replace(WorkspaceSyntaxEntry target, JsonNode original, JsonNode? replacement, SyntaxNode? value)
    {
        if (replacement is not null)
        {
            CarrySourceLocations(original, replacement);
            if (value is not null)
            {
                CarryReplacementMetadata(value, replacement);
            }
        }

        if (target.Parent is null)
        {
            _roots[target.Handle.Document] = replacement!;
        }
        else if (original.Parent is JsonArray array)
        {
            var position = array.IndexOf(original);
            if (replacement is null)
            {
                array.RemoveAt(position);
            }
            else
            {
                array[position] = replacement;
            }
        }
        else
        {
            original.Parent![target.Member!] = replacement;
        }
    }

    sealed record Edit(WorkspaceSyntaxEntry? Target, JsonNode? Original, Insertion? Destination, SyntaxNode? Value);
    sealed record Insertion(WorkspaceSyntaxEntry Parent, JsonObject Owner, string Member, int? Boundary, JsonNode? Anchor, bool Collection);
}
