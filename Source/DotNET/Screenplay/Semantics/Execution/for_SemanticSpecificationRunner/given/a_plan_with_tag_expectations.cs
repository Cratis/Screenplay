// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.given;

public class a_plan_with_tag_expectations : a_bound_register_project_plan
{
    protected ExecutableSemanticModel Model(string[][] tags, string[]?[] expectedTags, bool unordered = false)
    {
        var original = _plan.Specifications.Values.Single(spec => spec.ThenQueries.Length > 0);
        var application = _plan.Model.Application;
        var module = application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single(value => value.Specifications.Any(spec => spec.Id == original.Id));
        var command = slice.Commands.Single();
        var specification = original with
        {
            ThenEvents = [.. expectedTags.Select(expected => original.ThenEvents.Single() with { ExpectedTags = expected is null ? null : [.. expected] })],
            ThenEventsInAnyOrder = unordered,
            ThenReadModels = [],
            ThenQueries = []
        };
        var updated = slice with
        {
            Commands = [command with { Produces = [.. tags.Select(actual => command.Produces.Single() with { Tags = [.. actual] })] }],
            Specifications = [.. slice.Specifications.Select(value => value.Id == original.Id ? specification : value)]
        };
        var checksTags = expectedTags.Any(expected => expected is not null);
        return ExecutableSemanticModel.Create(
            checksTags ? LanguageVersion.V54 : LanguageVersion.V53,
            checksTags ? SemanticVersion.V54 : SemanticVersion.V53,
            application with { Modules = [module with { Features = [feature with { Slices = [.. feature.Slices.Select(value => value.Id == slice.Id ? updated : value)] }] }] });
    }

    protected bool Passes(string[][] tags, string[]?[] expectedTags, bool unordered = false)
    {
        var plan = SemanticExecutionPlan.Compile(Model(tags, expectedTags, unordered)).Plan!;
        var specification = plan.Specifications.Values.Single(spec => spec.ThenEvents.Length > 0);
        return new SemanticSpecificationRunner().Run(plan, specification.Id).Passed;
    }
}
