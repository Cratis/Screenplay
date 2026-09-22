// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Captures;
using Cratis.Screenplay.Syntax.Projections;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Tool.Mcp;

static class McpReferenceKinds
{
    internal static (string Name, string[] Kinds)? For(SyntaxNode node) => node switch
    {
        ProducesSyntax value => (value.Event, ["Event"]),
        InvokesSyntax value => (value.Command, ["Command"]),
        ReadsSyntax value => (value.ReadModel, ["ReadModel", "Projection"]),
        PolicyReferenceSyntax value => (value.Name, ["Policy"]),
        TypeRefSyntax value when !ConceptSyntax.PrimitiveTypes.Contains(value.Name, StringComparer.Ordinal) => (value.Name, ["Concept", "Type", "ReadModel", "Projection"]),
        ScreenDataSyntax value => (value.Query, ["Query"]),
        ScreenActionSyntax value => (value.Command, ["Command"]),
        ScreenNavigateSyntax value => (value.Screen, ["Screen"]),
        ScreenTemplateReferenceSyntax value => (value.Name, ["ScreenTemplate", "DialogTemplate"]),
        FormSyntax value => (value.For, ["Command"]),
        FormPopulateViaQuerySyntax value => (value.Query, ["Query"]),
        SpecificationEventSyntax value => (value.EventType, ["Event"]),
        SpecificationCommandSyntax value => (value.CommandType, ["Command"]),
        SpecificationReadModelSyntax value => (value.Name, ["ReadModel", "Projection"]),
        SpecificationQuerySyntax value => (value.Query, ["Query"]),
        EventSpecSyntax value => (value.Event, ["Event"]),
        JoinEventSyntax value => (value.Event, ["Event"]),
        ClearWithSyntax value => (value.Event, ["Event"]),
        ProjectionEntersOnSyntax value => (value.Event, ["Event"]),
        ProjectionSyntax { ReadModel: not null } value => (value.ReadModel, ["ReadModel"]),
        ReducerSyntax value => (value.ReadModel, ["ReadModel"]),
        ReducerRuleSyntax value => (value.Event, ["Event"]),
        NamedTriggerSourceSyntax value => (value.Name, ["Event", "Trigger"]),
        CaptureAppendSyntax value => (value.Event, ["Event"]),
        UniqueEventConstraintSyntax value => (value.Event, ["Event"]),
        SeedEventSyntax value => (value.Event, ["Event"]),
        _ => null
    };
}
