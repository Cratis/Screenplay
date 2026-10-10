// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes;

public class with_a_production_override : event_route_models
{
    SemanticExecutionResult _result = null!;

    void Because()
    {
        var application = Plan().Model.Application;
        var command = Command with
        {
            Produces = [Command.Produces[0] with { Route = new(Source.Id, Stream.Id) { StreamId = SemanticExpression.FromValue(SemanticValue.Text("override")) } }]
        };
        var module = application.Modules[0];
        var feature = module.Features[0];
        application = application with { Modules = [module with { Features = [feature with { Slices = [feature.Slices[0] with { Commands = [command] }] }] }] };
        var model = ExecutableSemanticModel.Create(LanguageVersion.V10, SemanticVersion.V10, application);
        _result = new SemanticEvaluator().Execute(SemanticExecutionPlan.Compile(model).Plan!, SemanticWorld.Empty, Request(SemanticValue.Text("inherited")));
    }

    [Fact] void should_accept() => (_result is SemanticAccepted).ShouldBeTrue();
    [Fact] void should_replace_the_entire_command_route() => ((SemanticAccepted)_result).Facts[0].Route.ShouldEqual(new SemanticEventRoute("stored-project", "stored-ledger", "override"));
}
