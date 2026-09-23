// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Mcp;

static class McpReferenceKinds
{
    internal const string Coverage = "Explicit declaration references in types, policies, commands, queries, screen data, forms, contribution points, specifications, projections, reducers, reactions, captures, constraints, seeds and concurrency event lists. Not code, property paths, imports, profile settings, external host registrations or expression identifiers. Read models include projection output aliases and variants, not the projection builder's name when it produces a different view.";
    static readonly string[] _eventKinds = ["Event"];

    internal static IEnumerable<(string Name, string[] Kinds, string Role)> For(SyntaxNode node, SyntaxNode? owner = null) => node switch
    {
        ProducesSyntax value => [(value.Event, ["Event"], "produces")],
        InvokesSyntax value => [(value.Command, ["Command"], "invokes")],
        ReadsSyntax value => [(value.ReadModel, ["ReadModel"], "reads")],
        ConcurrencySyntax value => value.EventTypes.Select(name => (name, _eventKinds, "concurrency")),
        PolicyReferenceSyntax value => [(value.Name, ["Policy"], "authorizes")],
        TypeRefSyntax value when owner is QuerySyntax query && ReferenceEquals(query.ReturnType, value) && !ConceptSyntax.PrimitiveTypes.Contains(value.Name, StringComparer.Ordinal) => [(value.Name, ["ReadModel", "Concept", "Type"], "queryResult")],
        TypeRefSyntax value when owner is ScreenSyntax && !ConceptSyntax.PrimitiveTypes.Contains(value.Name, StringComparer.Ordinal) => [(value.Name, ["ReadModel"], "dataReadModel")],
        TypeRefSyntax value when !ConceptSyntax.PrimitiveTypes.Contains(value.Name, StringComparer.Ordinal) => [(value.Name, ["Concept", "Type"], "type")],
        ScreenDataSyntax value => [(value.Query, ["Query"], "dataQuery")],
        ContributionSyntax value => [(value.ContributionPoint, ["ContributionPoint"], "contributes")],
        ScreenActionSyntax value => [(value.Command, ["Command"], "action")],
        ScreenNavigateSyntax value => [(value.Screen, ["Screen"], "navigate")],
        ScreenTemplateReferenceSyntax value => [(value.Name, ["ScreenTemplate", "DialogTemplate"], "template")],
        FormSyntax value => [(value.For, ["Command"], "formCommand")],
        FormPopulateViaQuerySyntax value => [(value.Query, ["Query"], "populate")],
        SpecificationEventSyntax value => [(value.EventType, ["Event"], "specificationEvent")],
        SpecificationCommandSyntax value => [(value.CommandType, ["Command"], "whenCommand")],
        SpecificationReadModelSyntax value => [(value.Name, ["ReadModel"], "specificationReadModel")],
        SpecificationQuerySyntax value => [(value.Query, ["Query"], "thenQuery")],
        CompositeKeySyntax value => [(value.Type, ["Type", "Concept"], "compositeKeyType")],
        EventSpecSyntax value => [(value.Event, ["Event"], "from")],
        JoinEventSyntax value => [(value.Event, ["Event"], "join")],
        ClearWithSyntax value => [(value.Event, ["Event"], "clear")],
        RemoveWithSyntax value => [(value.Event, ["Event"], "remove")],
        RemoveViaJoinSyntax value => [(value.Event, ["Event"], "removeViaJoin")],
        ProjectionEntersOnSyntax value => [(value.Event, ["Event"], "entersOn")],
        ProjectionSyntax value when !value.Blocks.OfType<ProjectionVariantSyntax>().Any() => [(value.ReadModel ?? value.Name, ["ReadModel"], "builds")],
        ProjectionVariantSyntax value => [(value.Name, ["ReadModel"], "buildsVariant")],
        ReducerSyntax value => [(value.ReadModel, ["ReadModel"], "builds")],
        ReducerRuleSyntax value => [(value.Event, ["Event"], "reduces")],
        NamedTriggerSourceSyntax value => [(value.Name, ["Event", "Trigger"], "trigger")],
        CaptureAppendSyntax value => [(value.Event, ["Event"], "appends")],
        UniqueEventConstraintSyntax value => [(value.Event, ["Event"], "uniqueEvent")],
        UniquePropertyConstraintSyntax value => [(value.Event, ["Event"], "uniqueProperty")],
        SeedEventSyntax value => [(value.Event, ["Event"], "seed")],
        _ => []
    };
}
