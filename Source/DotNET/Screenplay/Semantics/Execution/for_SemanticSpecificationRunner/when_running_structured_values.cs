// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_structured_values : for_SemanticModelBinder.when_binding_structured_specification_values.given.a_structured_specification
{
    SemanticSpecificationRun _result;
    SemanticSpecificationRun _commandResult;

    void Because()
    {
        var compilation = Compile(Source);
        var plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
        var specification = plan.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single(value => value.When is null);
        var commandSpecification = plan.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single(value => value.When is not null);
        var runner = new SemanticSpecificationRunner();
        _result = runner.Run(plan, specification.Id);
        _commandResult = runner.Run(plan, commandSpecification.Id);
    }

    [Fact] void should_pass_a_list_property_subset_assertion() => _result.Passed.ShouldBeTrue();
    [Fact] void should_pass_structured_command_and_event_values() => string.Join("; ", _commandResult.Failures).ShouldEqual(string.Empty);
}
