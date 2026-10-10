// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_a_reaction_filters_by_stream : event_route_models
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_observe_only_matching_routed_facts(bool routed)
    {
        var reaction = new SemanticReaction(SemanticId.Create(SemanticAddress.ForReaction(AutomationAddress, "Follow")), "Follow",
            [new(SemanticReactionTriggerKind.Event) { Source = Event.Id, Produces = [new(Before.Id, null, null, [])] }]);
        var address = SemanticAddress.ForReadModel(SliceAddress, "Unchanged");
        var readModel = new SemanticReadModel(SemanticId.Create(address), "Unchanged", [new(SemanticId.Create(SemanticAddress.ForProperty(address, "id")), "id", Text, true)]);
        var specification = Specification() with
        {
            GivenEvents = [new(Event.Id, []) { EventSource = new(Text, SemanticValue.Text("history")) }],
            When = null,
            WhenAppended = new(Event.Id, []) { EventSource = new(Text, SemanticValue.Text("project-1")), Route = routed ? Fixture(SemanticValue.Text("period")) : null },
            ThenEvents = routed ? [new(Before.Id, []) { Unrouted = true }] : [],
            ThenAbsentReadModels = [new(readModel.Id, SemanticValue.Text("absent"))]
        };
        var application = Plan(reactions: [reaction]).Model.Application;
        var module = application.Modules[0];
        var feature = module.Features[0];
        application = application with { Modules = [module with { Features = [feature with { Slices = [feature.Slices[0] with { ReadModels = [readModel], Specifications = [specification] }, feature.Slices[1] with { Reactions = [reaction with { From = new(Source.Id, Stream.Id) }] }] }] }] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
        var result = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        result.Passed.ShouldBeTrue();
        result.Execution.World.Facts.Count(fact => fact.EventContract == Before.Id).ShouldEqual(routed ? 1 : 0);
    }
}
