// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_comparing_produced_events_in_any_order : given.a_bound_register_project_plan
{
    SemanticSpecificationRun _ordered;
    SemanticSpecificationRun _unordered;

    void Because()
    {
        var original = _plan.Specifications.Values.Single(spec => spec.ThenQueries.Length > 0);
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(value => value.Specifications.Any(spec => spec.Id == original.Id));
        var command = slice.Commands.Single();
        var production = command.Produces.Single();
        var eventContract = slice.Events.Single(value => value.Id == production.EventContract);
        var nameProperty = eventContract.Properties.Single(value => value.Name == "name");
        var otherName = SemanticValue.Text("Another name");
        var otherProduction = production with
        {
            Mappings = [.. production.Mappings.Select(value => value.TargetProperty != nameProperty.Id ? value : value with
            {
                Source = SemanticExpression.FromValue(otherName)
            })]
        };
        var otherExpected = original.ThenEvents.Single() with
        {
            Values = [.. original.ThenEvents.Single().Values.Select(value => value.TargetProperty != nameProperty.Id ? value : value with { Value = otherName })]
        };
        var reversed = original with
        {
            ThenEvents = [otherExpected, original.ThenEvents.Single()],
            ThenReadModels = [],
            ThenQueries = []
        };
        SemanticSpecificationRun Run(bool unordered)
        {
            var expected = reversed with { ThenEventsInAnyOrder = unordered };
            var updated = slice with
            {
                Commands = [command with { Produces = [production, otherProduction] }],
                Specifications = [.. slice.Specifications.Select(value => value.Id == original.Id ? expected : value)]
            };
            var model = ExecutableSemanticModel.Create(
                _plan.Model.LanguageVersion,
                _plan.Model.SemanticVersion,
                application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? updated : value)] }] }] });
            return new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, expected.Id);
        }

        _ordered = Run(false);
        _unordered = Run(true);
    }

    [Fact] void should_reject_reversed_facts_by_default() => _ordered.Passed.ShouldBeFalse();
    [Fact] void should_accept_reversed_facts_with_the_qualifier() => _unordered.Passed.ShouldBeTrue();
}
