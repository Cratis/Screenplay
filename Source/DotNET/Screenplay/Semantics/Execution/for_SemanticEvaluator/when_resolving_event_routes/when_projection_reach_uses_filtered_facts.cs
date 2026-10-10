// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

public class when_projection_reach_uses_filtered_facts : event_route_models
{
    [Fact]
    void should_not_combine_a_matching_unrelated_fact_with_an_excluded_projection_event()
    {
        var application = Plan().Model.Application;
        var address = SemanticAddress.ForReadModel(SliceAddress, "MixedBalance");
        var readModel = new SemanticReadModel(SemanticId.Create(address), "MixedBalance", [new(SemanticId.Create(SemanticAddress.ForProperty(address, "id")), "id", Text, true)]);
        var reducer = new SemanticReducer("Fold", readModel.Id, [new(Before.Id, "opaque-transition")]) { From = new(Source.Id, Stream.Id) };
        var projection = new SemanticProjection(SemanticId.Create(SemanticAddress.ForProjection(SliceAddress, "MixedProjection")), "MixedProjection", readModel.Id, [])
        {
            Scope = new([new(Before.Id, SemanticProjectionKey.EventSourceIdentity, null, [])], [], [], [], null, [], [])
        };
        var command = Command with { Produces = [Command.Produces[0], new(Before.Id, null, Property(Identity), [])] };
        var module = application.Modules[0];
        var feature = module.Features[0];
        application = application with { Modules = [module with { Features = [feature with { Slices = [feature.Slices[0] with { Commands = [command], ReadModels = [readModel], Reducers = [reducer], Projections = [projection] }] }] }] };
        var plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application)).Plan!;
        var result = new SemanticEvaluator().EstablishWorld(plan, [Fact(new("stored-project", "stored-ledger", "period")), Fact(null, Before.Id)]);
        result.ShouldBeOfExactType<SemanticAccepted>();
    }
}
