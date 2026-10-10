// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

public class failing_in_a_skipped_production : event_route_models
{
    SemanticExecutionResult _result = null!;

    void Because()
    {
        var application = Plan().Model.Application;
        var command = Command with
        {
            Route = new(Source.Id, Stream.Id) { StreamId = SemanticExpression.FromValue(SemanticValue.Text("valid")) },
            Produces = [Command.Produces[0], Command.Produces[0] with
            {
                Condition = SemanticExpression.FromValue(SemanticValue.Boolean(false)),
                Route = new(Source.Id, Stream.Id) { StreamId = Property(Key) }
            }]
        };
        var module = application.Modules[0];
        var feature = module.Features[0];
        application = application with { Modules = [module with { Features = [feature with { Slices = [feature.Slices[0] with { Commands = [command] }] }] }] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
        _result = new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, Request(SemanticValue.Text("")));
    }

    [Fact] void should_reject_before_producing_any_fact() => (_result is SemanticRejected { Category: SemanticRejectionCategory.Contract }).ShouldBeTrue();
    [Fact] void should_leave_the_world_unchanged() => _result.World.Facts.ShouldBeEmpty();
}
