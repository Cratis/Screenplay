// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_establishing_state_from_given_events : a_valid_semantic_model
{
    SemanticSpecificationRun _result;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Specifications.Length > 0);
        var original = slice.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var success = original with { GivenEvents = original.ThenEvents, GivenReadModels = [] };
        var model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with
            {
                Specifications = [.. slice.Specifications.Select(value => value.Id == success.Id ? success : value)]
            }));
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        _result = new SemanticSpecificationRunner().Run(plan, success.Id);
    }

    [Fact] void should_pass_the_specification() => _result.Passed.ShouldBeTrue();
    [Fact] void should_replay_and_commit_both_facts() => _result.Execution.World.Facts.Length.ShouldEqual(2);
    [Fact] void should_establish_the_projected_state() => _result.Execution.World.ReadModels.Single().ReadModel.ShouldEqual(_readModelId);
}
