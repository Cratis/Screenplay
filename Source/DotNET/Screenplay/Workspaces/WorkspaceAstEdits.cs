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
    readonly Dictionary<JsonNode, WorkspaceSyntaxEntry> _ruleOrigins = [];
    readonly Dictionary<SyntaxNode, WorkspaceSyntaxEntry?> _expectedOrigins = new(ReferenceEqualityComparer.Instance);
    readonly Dictionary<JsonNode, ImmutableArray<SourceComment>> _sourceComments = [];
    readonly Dictionary<JsonNode, IReadOnlyDictionary<string, SourceLocation>> _directiveLocations = [];
    readonly Dictionary<JsonNode, AutoMapMode> _parsedAutoMapModes = [];
    readonly List<(WorkspaceSyntaxEntry Entry, JsonNode Node)> _originals = [];
    readonly List<(WorkspaceSyntaxEntry Target, JsonNode Original, JsonNode Replacement)> _replaced = [];
    readonly List<(WorkspaceSyntaxEntry Target, JsonNode Original, JsonNode Inserted)> _moved = [];

    internal ImmutableArray<DocumentId> Touched => [.. _edits.SelectMany(edit => new[] { edit.Target?.Handle.Document, edit.Destination?.Parent.Handle.Document }).OfType<DocumentId>().Distinct()];

    internal IEnumerable<(WorkspaceSyntaxEntry Target, SyntaxNode Replacement)> Replacements =>
        _edits.Where(edit => edit.Target is not null && edit.Destination is null && edit.Value is not null).Select(edit => (edit.Target!, edit.Value!));

    internal IEnumerable<WorkspaceSyntaxEntry> PendingRuleRemovals => _edits.Where(IsPendingRuleRemoval).Select(edit => edit.Target!);

    internal static bool Contains(WorkspaceNodeHandle ancestor, WorkspaceNodeHandle descendant) => ancestor.Document == descendant.Document &&
        (ancestor.Path == descendant.Path || descendant.Path.StartsWith($"{ancestor.Path}/", StringComparison.Ordinal));

    internal bool OnlyPendingRuleRemovals(DocumentId document) => _edits.Where(edit => edit.Target?.Handle.Document == document || edit.Destination?.Parent.Handle.Document == document).All(IsPendingRuleRemoval);

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
            _originals.Add((entry, Resolve(entry.Handle)));
            _expectedOrigins.TryAdd(entry.Node, entry);
            _sourceLocations[Resolve(entry.Handle)] = entry.Location;
            if (entry.Node is ValidationRuleSyntax)
            {
                _ruleOrigins[Resolve(entry.Handle)] = entry;
            }
            _sourceComments[Resolve(entry.Handle)] = entry.Node.SourceComments;
            _directiveLocations[Resolve(entry.Handle)] = entry.Node.DirectiveLocations;
            if (entry.Node.ParsedAutoMapMode is { } mode)
            {
                _parsedAutoMapModes[Resolve(entry.Handle)] = mode;
            }
        }

        foreach (var edit in _edits.Where(edit => edit.Target is not null))
        {
            // The original removal handle is validated, but its ancestor replacement supplies final source.
            // Do not mutate the captured original subtree: other correspondence still uses original paths.
            if (IsPendingRuleRemoval(edit) && _edits.Exists(other => other.Value is not null && other.Destination is null && other.Target is not null && Contains(other.Target.Handle, edit.Target!.Handle)))
            {
                continue;
            }

            var replacement = edit.Destination is null && edit.Value is not null ? ToJson(edit.Value) : null;
            Replace(edit.Target!, edit.Original!, replacement, edit.Value);
            if (replacement is not null)
            {
                _replaced.Add((edit.Target!, edit.Original!, replacement));
            }
        }

        foreach (var edit in _edits.Where(edit => edit.Destination is not null))
        {
            var inserted = ToJson(edit.Value!);
            if (edit.Target is not null)
            {
                CarrySourceLocations(edit.Original!, inserted, ruleLineage: true);
                if (edit.Target.Handle.Document != edit.Destination!.Parent.Handle.Document || !LocationAgreesWithInsertion(edit.Destination, inserted))
                {
                    // Keep subtree order and comment/directive anchors, but do not let the old root
                    // position override the requested order among the destination's same-kind siblings.
                    _sourceLocations.Remove(inserted);
                }
            }

            // An add is a new occurrence, even when its value is an existing node or a with-copy.
            Insert(edit.Destination!, inserted);
            if (edit.Target is not null)
            {
                _moved.Add((edit.Target, edit.Original!, inserted));
            }
        }

        return Touched.ToDictionary(
            document => document,
            document => RestoreSourceLocations(
                _roots[document],
                SyntaxJson.Deserialize(JsonSerializer.SerializeToElement(_roots[document], _jsonOptions)) as ApplicationSyntax
                    ?? throw new InvalidWorkspaceAuthoring("A document root must remain an ApplicationSyntax.")));
    }

    /// <summary>
    /// Records the provenance of every occurrence in the applied documents. Only the applied operations establish
    /// correspondence; see <see cref="WorkspaceEditProvenance"/>.
    /// </summary>
    /// <param name="provenance">The transaction provenance to extend.</param>
    /// <param name="after">The candidate occurrence index.</param>
    internal void Record(WorkspaceEditProvenance provenance, WorkspaceSyntaxIndex after)
    {
        var positions = new Dictionary<JsonNode, (DocumentId Document, string Path)>(ReferenceEqualityComparer.Instance);
        foreach (var document in Touched)
        {
            Walk(_roots[document], document, string.Empty, positions);
        }

        foreach (var (entry, node) in _originals.Where(original => Touched.Contains(original.Entry.Handle.Document)))
        {
            if (positions.TryGetValue(node, out var position))
            {
                provenance.Map(position, (entry.Handle.Document, entry.Handle.Path));
            }
        }

        foreach (var (target, original, inserted) in _moved)
        {
            if (positions.TryGetValue(inserted, out var position))
            {
                provenance.Identical(original, (target.Handle.Document, target.Handle.Path), inserted, position);
            }
        }

        foreach (var (target, original, replacement) in _replaced)
        {
            if (positions.TryGetValue(replacement, out var position))
            {
                provenance.Replacement(original, (target.Handle.Document, target.Handle.Path), replacement, position);
            }
        }

        foreach (var (target, _, replacement) in _replaced)
        {
            if (positions.TryGetValue(replacement, out var position))
            {
                provenance.Region(index, (target.Handle.Document, target.Handle.Path), after, position);
            }
        }
    }

    // Source/comment correspondence is stricter than assuming every final collection index is an
    // identity. Use the actual metadata carried by the edit engine, independently of reference provenance.
    internal void RecordPendingRuleSources(WorkspaceEditProvenance provenance, IEnumerable<ReplaceWorkspaceSyntaxDocument> replacements)
    {
        var replaced = replacements.ToArray();
        var documents = replaced.Select(replacement => replacement.Document).ToHashSet();
        var positions = new Dictionary<JsonNode, (DocumentId Document, string Path)>(ReferenceEqualityComparer.Instance);
        foreach (var (document, root) in _roots.Where(pair => !documents.Contains(pair.Key)))
        {
            Walk(root, document, string.Empty, positions);
        }

        foreach (var replacement in replaced)
        {
            var root = ToJson(replacement.Syntax);

            // Parser-invalid documents have no original syntax occurrences to correspond or protect.
            if (_roots.TryGetValue(replacement.Document, out var original))
            {
                CarrySourceLocations(original, root);
            }

            CarryReplacementMetadata(replacement.Syntax, root);
            Walk(root, replacement.Document, string.Empty, positions);
        }

        foreach (var group in _ruleOrigins.Where(pair => positions.ContainsKey(pair.Key)).GroupBy(pair => pair.Value.Handle))
        {
            // Reusing one original value in several candidates does not prove which occurrence survived.
            var matches = group.ToArray();
            if (matches.Length == 1)
            {
                var match = matches[0];
                provenance.Map(positions[match.Key], (match.Value.Handle.Document, match.Value.Handle.Path));
            }
            else
            {
                foreach (var match in matches)
                {
                    provenance.Ambiguous(positions[match.Key], (match.Value.Handle.Document, match.Value.Handle.Path));
                }
            }
        }
    }

    static bool IsPendingRuleRemoval(Edit edit) => edit is { Target.Node: ValidationRuleSyntax { Implementation: not null, File: null, Code: null }, Destination: null, Value: null };

    static void Walk(JsonNode? node, DocumentId document, string path, Dictionary<JsonNode, (DocumentId Document, string Path)> positions)
    {
        if (node is JsonObject owner)
        {
            positions[owner] = (document, path);
            foreach (var (name, value) in owner)
            {
                Walk(value, document, $"{path}/{name}", positions);
            }
        }
        else if (node is JsonArray items)
        {
            for (var position = 0; position < items.Count; position++)
            {
                Walk(items[position], document, $"{path}/{position}", positions);
            }
        }
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

    bool LocationAgreesWithInsertion(Insertion destination, JsonNode node)
    {
        if (!destination.Collection || destination.Owner[destination.Member] is not JsonArray siblings)
        {
            return true;
        }

        if (!_sourceLocations.TryGetValue(node, out var location) || location is not { Line: > 1, Column: > 0 })
        {
            return false;
        }

        var boundary = destination.Anchor is null ? siblings.Count : siblings.IndexOf(destination.Anchor);
        for (var position = 0; position < siblings.Count; position++)
        {
            if (siblings[position] is not { } sibling || !_sourceLocations.TryGetValue(sibling, out var other) || other is not { Line: > 1, Column: > 0 })
            {
                // The printer places unlocated siblings after all located members of their kind.
                if (position < boundary) return false;
                continue;
            }

            var order = location.Line.CompareTo(other.Line);
            if (order == 0) order = location.Column.CompareTo(other.Column);
            if (location.Path != other.Path || (position < boundary ? order < 0 : order > 0))
            {
                return false;
            }
        }

        return true;
    }

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

        RegisterExpected(expected, handle);
        return entry;
    }

    // The expectation is validated against the complete original subtree before these actual object
    // references are used. This is occurrence proof, unlike a location borrowed from newly parsed source.
    void RegisterExpected(SyntaxNode node, WorkspaceNodeHandle handle)
    {
        var entry = index.Find(handle);
        if (_expectedOrigins.TryGetValue(node, out var previous) && previous?.Handle != handle)
        {
            _expectedOrigins[node] = null;
        }
        else
        {
            _expectedOrigins[node] = entry;
        }

        foreach (var member in SyntaxKinds.All.Single(kind => kind.Type == node.GetType()).Members)
        {
            var value = member.Property.GetValue(node);
            if (value is SyntaxNode child)
            {
                RegisterExpected(child, handle with { Path = $"{handle.Path}/{member.Name}" });
            }
            else if (value is System.Collections.IEnumerable children and not string)
            {
                var position = 0;
                foreach (var item in children)
                {
                    if (item is SyntaxNode nested)
                    {
                        RegisterExpected(nested, handle with { Path = $"{handle.Path}/{member.Name}/{position}" });
                    }

                    position++;
                }
            }
        }
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
        var targets = _edits.Where(edit => edit.Target is not null).ToArray();
        for (var first = 0; first < targets.Length; first++)
        {
            for (var second = first + 1; second < targets.Length; second++)
            {
                if (Contains(targets[first].Target!.Handle, targets[second].Target!.Handle) || Contains(targets[second].Target!.Handle, targets[first].Target!.Handle))
                {
                    // An explicit pending-rule removal may precede replacement of a strict ancestor.
                    // Every other overlap keeps its existing refusal, including reversing this order.
                    if (IsPendingRuleRemoval(targets[first]) && targets[second] is { Value: not null, Destination: null } &&
                        targets[first].Target!.Handle != targets[second].Target!.Handle && Contains(targets[second].Target!.Handle, targets[first].Target!.Handle))
                    {
                        continue;
                    }

                    throw new InvalidWorkspaceAuthoring("AST edits overlap an original node or its descendants.");
                }
            }
        }

        var destinations = _edits.Where(edit => edit.Destination is not null).Select(edit => edit.Destination!).ToArray();
        foreach (var destination in destinations)
        {
            if (targets.Any(target => Contains(target.Target!.Handle, destination.Parent.Handle)))
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
            CarrySourceLocations(original, replacement, ruleLineage: target.Node is ValidationRuleSyntax);
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
            if (replacement is null && target.Node is ImplementationSyntax && target.Parent is { } parent && index.Find(parent)?.Node is ValidationRuleSyntax)
            {
                // Unwrapping removes the guidance nodes, not their source comments. Keep them on the
                // surviving rule; final-source validation decides whether the atomic transition is valid.
                var owner = original.Parent!;
                _sourceComments[owner] = [.. _sourceComments.GetValueOrDefault(owner, []), .. _originals
                    .Where(item => Contains(target.Handle, item.Entry.Handle))
                    .SelectMany(item => item.Entry.Node.SourceComments)];
            }

            original.Parent![target.Member!] = replacement;
        }
    }

    sealed record Edit(WorkspaceSyntaxEntry? Target, JsonNode? Original, Insertion? Destination, SyntaxNode? Value);
    sealed record Insertion(WorkspaceSyntaxEntry Parent, JsonObject Owner, string Member, int? Boundary, JsonNode? Anchor, bool Collection);
}
