// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_a_whenless_billing_lookup : given.a_bound_register_project_plan
{
    SemanticSpecificationRun _result;

    void Because()
    {
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(_ => _.Specifications.Any(value => value.ThenQueries.Length > 0));
        var original = slice.Specifications.Single(_ => _.ThenQueries.Length > 0);
        var readModel = original.ThenReadModels.Single();
        var identifier = _plan.ReadModels[readModel.ReadModel].Properties.Single(_ => _.IsIdentifier).Id;
        var subset = readModel with { Values = [.. readModel.Values.Where(_ => _.TargetProperty == identifier)] };
        var query = original.ThenQueries.Single() with
        {
            Results = [readModel with { Values = [.. readModel.Values.Where(_ => _.TargetProperty != identifier)] }]
        };
        var readOnly = original with
        {
            When = null,
            GivenReadModels = [readModel],
            ThenEvents = [],
            ThenReadModels = [subset],
            ThenQueries = [query]
        };
        var changed = slice with { Specifications = [.. slice.Specifications.Select(_ => _.Id == original.Id ? readOnly : _)] };
        var model = ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(_ => _.Id == slice.Id ? changed : _)] }] }] });
        _result = new SemanticSpecificationRunner().Run(SemanticExecutionPlan.Compile(model).Plan!, readOnly.Id);
    }

    [Fact] void should_pass_subset_read_model_and_keyless_query_assertions() => _result.Passed.ShouldBeTrue();
    [Fact] void should_produce_no_facts_without_a_command() => ((SemanticAccepted)_result.Execution).Facts.ShouldBeEmpty();
}
