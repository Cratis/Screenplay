// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticEventRouting;

public class when_inferring_refusal_branch_destinations : Specification
{
    static readonly ApplicationIdentity Application = ApplicationIdentity.Create("Refusals");
    static readonly SemanticTypeReference Text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
    static readonly SemanticTypeReference Uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_include_explicit_and_inherited_refusal_destinations(bool explicitDestination)
    {
        var stream = new SemanticEventStream(Id("Stream"), "Ledger", "ledger");
        var source = new SemanticEventSource(Id("Source"), "Account", "account", [stream]);
        var production = new SemanticProducedEvent(Id("Refused"), null, null, []) { DestinationType = explicitDestination ? Text : null };
        var trigger = new SemanticReactionTrigger(SemanticReactionTriggerKind.Event)
        {
            Source = Id("Started"),
            Invokes = [new(Id("Claim"), []) { OnRefused = [new("validation", null, [production])] }]
        };
        var producer = new SemanticCommand(Id("Start"), "Start", [], [], [new(Id("Started"), null, null, [])]) { Destination = new(Uuid, null) };
        var slice = new SemanticSlice(Id("Slice"), "Slice", SemanticSliceKind.Automation, [], [producer], [], [], [], [])
        {
            Reactions = [new(Id("Reaction"), "Reaction", [trigger])]
        };
        var application = new SemanticApplication(Id("Application"), "Refusals", [], [], [new(Id("Module"), "Module", [new(Id("Feature"), "Feature", [], [slice])])])
        {
            EventSources = [source]
        };

        SemanticEventRouting.FixtureSourceType(application, new(source.Id, stream.Id), Id("Refused"), out var failure).ShouldEqual(explicitDestination ? Text : Uuid);
        failure.ShouldEqual(string.Empty);
    }

    static SemanticId Id(string name) => SemanticId.Create(SemanticAddress.ForConcept(Application, name));
}
