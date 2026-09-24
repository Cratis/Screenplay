// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_appending_an_event_and_comparing_properties : given.a_bound_register_project_plan
{
    SemanticSpecificationRun _subset;
    SemanticSpecificationRun _exact;
    SemanticSpecificationRun _assertedEvent;
    SemanticSpecificationRun _extraEvent;
    SemanticSpecificationRun _exactQuery;

    void Because()
    {
        var original = _plan.Specifications.Values.Single(spec => spec.ThenQueries.Length > 0);
        var fact = original.ThenEvents.Single();
        var readModel = original.ThenReadModels.Single();
        var identifier = _plan.ReadModels[readModel.ReadModel].Properties.Single(property => property.IsIdentifier).Id;
        var subset = readModel with { Values = [.. readModel.Values.Where(value => value.TargetProperty == identifier)] };
        var appended = original with
        {
            When = null,
            WhenAppended = new(fact.EventContract, fact.Values),
            ThenEvents = [],
            ThenReadModels = [subset],
            ThenQueries = []
        };
        SemanticSpecificationRun Run(SemanticSpecification specification)
        {
            var application = _plan.Model.Application;
            var module = application.Modules.Single();
            var feature = module.Features.Single();
            var slice = feature.Slices.Single(value => value.Specifications.Any(item => item.Id == original.Id));
            var updated = slice with { Specifications = [.. slice.Specifications.Select(value => value.Id == original.Id ? specification : value)] };
            var model = ExecutableSemanticModel.Create(
                _plan.Model.LanguageVersion,
                _plan.Model.SemanticVersion,
                application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? updated : value)] }] }] });
            return new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, specification.Id);
        }

        _subset = Run(appended);
        _exact = Run(appended with { ThenReadModels = [subset with { Exactly = true }] });
        _assertedEvent = Run(appended with { ThenEvents = [fact], ThenEventsInAnyOrder = true });
        _extraEvent = Run(appended with { ThenEvents = [fact, fact] });
        _exactQuery = Run(appended with { ThenQueries = [original.ThenQueries.Single() with
        {
            Results = [subset], Exactly = true
        }] });
    }

    [Fact] void should_project_appended_event_without_a_command() => _subset.Passed.ShouldBeTrue();
    [Fact] void should_produce_only_the_appended_fact() => ((SemanticAccepted)_subset.Execution).Facts.Length.ShouldEqual(1);
    [Fact] void should_reject_unasserted_properties_when_exact() => _exact.Passed.ShouldBeFalse();
    [Fact] void should_accept_an_unordered_event_assertion() => _assertedEvent.Passed.ShouldBeTrue();
    [Fact] void should_require_exactly_the_asserted_new_facts() => _extraEvent.Passed.ShouldBeFalse();
    [Fact] void should_reject_unasserted_query_row_properties_when_exact() => _exactQuery.Passed.ShouldBeFalse();
}
