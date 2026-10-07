// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Collects explicit slice references without resolving names or interpreting code and expressions.
/// </summary>
internal sealed class SliceReferences : ScreenplaySyntaxWalker
{
    internal static readonly IReadOnlyDictionary<string, string?> Classifications = new Dictionary<string, string?>(StringComparer.Ordinal)
    {
        ["from"] = "usesFactsFrom", ["join"] = "usesFactsFrom", ["remove"] = "usesFactsFrom", ["removeViaJoin"] = "usesFactsFrom",
        ["clear"] = "usesFactsFrom", ["entersOn"] = "usesFactsFrom", ["reduces"] = "usesFactsFrom", ["uniqueEvent"] = "usesFactsFrom",
        ["uniqueProperty"] = "usesFactsFrom", ["concurrency"] = "usesFactsFrom", ["trigger"] = "reactsTo", ["reads"] = "decidesFrom",
        ["invokes"] = "asks", ["action"] = "asks", ["actionAlternative"] = "asks", ["actionOtherwise"] = "asks", ["formCommand"] = "asks", ["dataQuery"] = "shows", ["populate"] = "shows", ["navigate"] = "shows",
        ["specificationEvent"] = "verifiedWith", ["givenEvent"] = "verifiedWith", ["whenAppendedEvent"] = "verifiedWith", ["thenEvent"] = "verifiedWith", ["whenCommand"] = "verifiedWith",
        ["declares"] = null, ["uses"] = null, ["commandEventSource"] = null, ["commandStream"] = null, ["thenOperation"] = null,
        ["givenOperationFailure"] = null, ["thenCompensated"] = null, ["produces"] = null, ["authorizes"] = null, ["queryResult"] = null,
        ["dataReadModel"] = null, ["type"] = null, ["contributes"] = null, ["template"] = null, ["specificationReadModel"] = null,
        ["thenAbsentReadModel"] = null, ["thenReadModel"] = null, ["givenReadModel"] = null, ["thenQuery"] = null,
        ["compositeKeyType"] = null, ["builds"] = null, ["buildsVariant"] = null, ["appends"] = null, ["seed"] = null
    };
    readonly List<SliceReference> _references = [];
    readonly List<(string Kind, string Name)> _shared = [];
    SpecificationSyntax? _specification;
    bool _projection;

    // Stable ties preserve the timeline diagnostic's original reference order.
    // Graph evidence applies its own complete deterministic ordering after resolution.
    internal IReadOnlyList<SliceReference> References => [.. _references.OrderBy(reference => reference.Location.Line).ThenBy(reference => reference.Location.Column)];

    /// <inheritdoc/>
    public override void VisitProjection(ProjectionSyntax syntax)
    {
        _projection = true;
        base.VisitProjection(syntax);
        _projection = false;
    }

    /// <inheritdoc/>
    public override void VisitSpecification(SpecificationSyntax syntax)
    {
        _specification = syntax;
        base.VisitSpecification(syntax);
        _specification = null;
    }

    /// <inheritdoc/>
    public override void VisitNode(SyntaxNode node)
    {
        if (node is TypeRefSyntax type && !ConceptSyntax.PrimitiveTypes.Contains(type.Name, StringComparer.Ordinal)) _shared.Add(("type", type.Name));
        if (node is PolicyReferenceSyntax policy) _shared.Add(("policy", policy.Name));
        var reference = node switch
        {
            EventSpecSyntax value => Reference(value.Event, "Event", "from", node),
            JoinEventSyntax value => Reference(value.Event, "Event", "join", node),
            RemoveWithSyntax value => Reference(value.Event, "Event", "remove", node),
            RemoveViaJoinSyntax value => Reference(value.Event, "Event", "removeViaJoin", node),
            ClearWithSyntax value => Reference(value.Event, "Event", "clear", node),
            ProjectionEntersOnSyntax value => Reference(value.Event, "Event", "entersOn", node),
            ReducerRuleSyntax value => Reference(value.Event, "Event", "reduces", node),
            UniqueEventConstraintSyntax value => Reference(value.Event, "Event", "uniqueEvent", node),
            UniquePropertyConstraintSyntax value => Reference(value.Event, "Event", "uniqueProperty", node),
            NamedTriggerSourceSyntax value => Reference(value.Name, "Event", "trigger", node),
            ReadsSyntax value => Reference(value.ReadModel, "ReadModel", "reads", node),
            InvokesSyntax value => Reference(value.Command, "Command", "invokes", node),
            ScreenActionSyntax value => Reference(value.Command, "Command", "action", node),
            ScreenActionAlternativeSyntax value => Reference(value.Command, "Command", "actionAlternative", node),
            ScreenActionOtherwiseSyntax { Command: { } command } => Reference(command, "Command", "actionOtherwise", node),
            FormSyntax value => Reference(value.For, "Command", "formCommand", node),
            ScreenDataSyntax value => Reference(value.Query, "Query", "dataQuery", node),
            FormPopulateViaQuerySyntax value => Reference(value.Query, "Query", "populate", node),
            ScreenNavigateSyntax value => Reference(value.Screen, "Screen", "navigate", node),
            SpecificationEventSyntax value => Reference(value.EventType, "Event", EventRole(node), node),
            SpecificationCommandSyntax value => Reference(value.CommandType, "Command", "whenCommand", node),
            _ => null
        };
        if (reference is not null) _references.Add(reference);
        if (node is ConcurrencySyntax concurrency)
        {
            _references.AddRange(concurrency.EventTypes.Select(name => Reference(name, "Event", "concurrency", node)));
        }
    }

    internal static IReadOnlyList<SliceReference> In(SliceSyntax slice) => In(slice, out _);

    internal static IReadOnlyList<SliceReference> In(SliceSyntax slice, out IReadOnlyList<(string Kind, string Name)> shared)
    {
        var collector = new SliceReferences();
        collector.VisitSlice(slice);
        shared = collector._shared;

        return collector.References;
    }

    string EventRole(SyntaxNode node)
    {
        if (_specification?.Given.Any(item => ReferenceEquals(item, node)) == true) return "givenEvent";

        return ReferenceEquals(_specification?.WhenAppended, node) ? "whenAppendedEvent" : "thenEvent";
    }

    SliceReference Reference(string name, string targetKind, string role, SyntaxNode node) =>
        new(name, targetKind, Classifications[role]!, role, node.Location, _projection || role == "trigger");
}

internal sealed record SliceReference(string Name, string TargetKind, string Kind, string Role, SourceLocation Location, bool Timeline);
