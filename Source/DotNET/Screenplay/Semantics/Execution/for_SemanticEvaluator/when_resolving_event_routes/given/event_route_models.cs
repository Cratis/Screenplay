// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

public class event_route_models : Specification
{
    internal static readonly ApplicationIdentity Application = ApplicationIdentity.Create("Routes");
    internal static readonly SemanticAddress SliceAddress = SemanticAddress.ForSlice(Application, "Module", "Feature", "Slice");
    internal static readonly SemanticAddress AutomationAddress = SemanticAddress.ForSlice(Application, "Module", "Feature", "Automation");
    internal static readonly SemanticAddress CommandAddress = SemanticAddress.ForCommand(SliceAddress, "Book");
    internal static readonly SemanticAddress SourceAddress = SemanticAddress.ForEventSource(Application, "Project");
    internal static readonly SemanticTypeReference Text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
    internal static readonly SemanticTypeReference Uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
    internal static readonly SemanticId IntegerId = SemanticId.Create(SemanticAddress.ForConcept(Application, "Period"));
    internal static readonly SemanticTypeReference Integer = SemanticTypeReference.ForConcept(IntegerId);
    internal static readonly SemanticId GeneratedId = SemanticId.Create(SemanticAddress.ForConcept(Application, "GeneratedId"));
    internal static readonly SemanticProperty Identity = new(SemanticId.Create(SemanticAddress.ForProperty(CommandAddress, "id")), "id", Text, true);
    internal static readonly SemanticProperty Key = new(SemanticId.Create(SemanticAddress.ForProperty(CommandAddress, "key")), "key", Text, false);
    internal static readonly SemanticEventContract Event = EventNamed("Booked");
    internal static readonly SemanticEventContract Before = EventNamed("Before");
    internal static readonly SemanticEventStream Stream = new(SemanticId.Create(SemanticAddress.ForEventStream(SourceAddress, "Ledger")), "Ledger", "stored-ledger") { StreamIdType = Text };
    internal static readonly SemanticEventSource Source = new(SemanticId.Create(SourceAddress), "Project", "stored-project", [Stream]) { IdentifierType = Text };
    internal static readonly SemanticCommand Command = new(SemanticId.Create(CommandAddress), "Book", [Identity, Key], [], [new(Event.Id, null, Property(Identity), [])])
    {
        Destination = new(Text, Property(Identity)),
        Route = new(Source.Id, Stream.Id) { StreamId = Property(Key) },
        Response = new SemanticScalarCommandResponse(Identity.Id, Text)
    };

    internal static SemanticResolvedExpression Property(SemanticProperty property, SemanticExpressionRootKind root = SemanticExpressionRootKind.Command) =>
        (SemanticResolvedExpression)SemanticExpression.Property(root, property.Id);

    internal static SemanticFixtureRoute Fixture(SemanticValue key) => new(Source.Id, Stream.Id) { StreamId = key };

    internal static SemanticSpecification Specification() => new(
        SemanticId.Create(SemanticAddress.ForSpecification(SliceAddress, "Scenario")), "Scenario", [], [],
        new(Command.Id, [new(Identity.Id, SemanticValue.Text("project-1")), new(Key.Id, SemanticValue.Text("2026-10"))]),
        [new(Event.Id, [])], [], [], []);

    internal static SemanticExecutionPlan Plan(
        SemanticCommand? command = null,
        SemanticEventSource? source = null,
        ImmutableArray<SemanticSpecification> specifications = default,
        ImmutableArray<SemanticReaction> reactions = default,
        bool noCommands = false,
        bool legacy = false)
    {
        var slice = new SemanticSlice(SemanticId.Create(SliceAddress), "Slice", SemanticSliceKind.StateChange,
            [Event, Before], noCommands ? [] : [command ?? Command], [], [], [], specifications.IsDefault ? [] : specifications);
        var automation = new SemanticSlice(SemanticId.Create(AutomationAddress), "Automation", SemanticSliceKind.Automation, [], [], [], [], [], [])
        {
            Reactions = reactions.IsDefault ? [] : reactions
        };
        var application = new SemanticApplication(SemanticId.Create(SemanticAddress.ForApplication(Application)), "Routes",
            [new(IntegerId, "Period", SemanticPrimitiveType.WholeNumber, [], []), new(GeneratedId, "GeneratedId", SemanticPrimitiveType.Uuid, [], [])], [],
            [new(SemanticId.Create(SemanticAddress.ForModule(Application, "Module")), "Module",
                [new(SemanticId.Create(SemanticAddress.ForFeature(Application, "Module", "Feature")), "Feature", [], reactions.IsDefaultOrEmpty ? [slice] : [slice, automation])])])
        {
            EventSources = legacy ? [] : [source ?? Source],
            Policies = [new("Authenticated", new SemanticAuthenticatedCondition())]
        };
        var model = ExecutableSemanticModel.Create(legacy ? LanguageVersion.V7 : LanguageVersion.V8,
            legacy ? SemanticVersion.V7 : SemanticVersion.V8, application);

        return SemanticExecutionPlan.Compile(model).Plan!;
    }

    internal static SemanticExecutionRequest Request(SemanticValue key) => SemanticExecutionRequest.Create(Command.Id,
        [new(Identity.Id, SemanticValue.Text("project-1")), new(Key.Id, key)], []);

    internal static SemanticFact Fact(SemanticEventRoute? route, SemanticId? contract = null) => new(contract ?? Event.Id, SemanticValue.Text("project-1"), [])
    {
        Context = new(new(Text, SemanticValue.Text("project-1"))),
        Route = route
    };

    static SemanticEventContract EventNamed(string name) => new(SemanticId.Create(SemanticAddress.ForEventContract(SliceAddress, name)),
        EventContractId.Create(Application, name), EventContractRevision.Initial, name, []);
}
