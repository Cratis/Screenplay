// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_a_reducer_filters_by_source : event_route_models
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    void should_reach_the_opaque_reducer_only_for_a_matching_fact(bool routed)
    {
        var application = Plan().Model.Application;
        var readModelAddress = SemanticAddress.ForReadModel(SliceAddress, "Balance");
        var readModel = new SemanticReadModel(SemanticId.Create(readModelAddress), "Balance",
            [new(SemanticId.Create(SemanticAddress.ForProperty(readModelAddress, "id")), "id", Text, true)]);
        var reducer = new SemanticReducer("Fold", readModel.Id, [new(Event.Id, "opaque-transition")]) { From = new(Source.Id) };
        var module = application.Modules[0];
        var feature = module.Features[0];
        application = application with { Modules = [module with { Features = [feature with { Slices = [feature.Slices[0] with { ReadModels = [readModel], Reducers = [reducer] }] }] }] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        var result = SemanticEvaluator.Append(plan, SemanticWorld.Empty, Fact(routed ? new("stored-project", "stored-ledger", "period") : null), [], null);
        (result is SemanticUnsupported).ShouldEqual(routed);
        (result is SemanticAccepted).ShouldEqual(!routed);
    }
}
