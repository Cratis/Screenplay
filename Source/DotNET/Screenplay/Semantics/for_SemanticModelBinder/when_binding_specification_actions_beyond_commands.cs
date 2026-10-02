// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_specification_actions_beyond_commands : given.a_semantic_binder
{
    const string Queried =
        """
        module Projects
          feature Lookup
            slice StateView Summaries
              readmodel ProjectSummary
                projectId Uuid
                name String
              query ProjectById => ProjectSummary?
                by projectId Uuid
              specification LookingUpAProject
                given readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                when query ProjectById
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then result
                  name = "Screenplay"
        """;

    const string Clocked =
        """
        module Projects
          feature Lookup
            slice StateView Summaries
              readmodel ProjectSummary
                projectId Uuid
                name String
              query ProjectById => ProjectSummary?
                by projectId Uuid
              specification LookingUpAProjectAtNine
                given clock "2026-10-05T09:00:00Z"
                given readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    CompilationResult<SemanticCompilation> _queried;
    CompilationResult<SemanticCompilation> _clocked;

    void Because()
    {
        _queried = Bind(Queried);
        _clocked = Bind(Clocked);
    }

    [Fact] void should_bind_a_performed_query() => _queried.Success.ShouldBeTrue();
    [Fact] void should_bind_it_as_a_query_assertion() => Specification.ThenQueries.Single().Results.Single().Values.Length.ShouldEqual(1);
    [Fact] void should_bind_it_as_read_only() => Specification.When.ShouldBeNull();
    [Fact] void should_bind_a_clock() => _clocked.Success.ShouldBeTrue();
    [Fact] void should_select_esm_v6_for_a_clock() => _clocked.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_bind_the_clock_to_its_round_trip_instant() => _clocked.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().GivenClock.ShouldEqual("2026-10-05T09:00:00.0000000Z");
    [Fact] void should_keep_a_performed_query_below_esm_v6() => _queried.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);

    SemanticSpecification Specification => _queried.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
