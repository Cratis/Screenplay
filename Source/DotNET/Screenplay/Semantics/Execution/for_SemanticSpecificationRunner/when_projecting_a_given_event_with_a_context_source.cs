// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_projecting_a_given_event_with_a_context_source : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId Uuid identifier
                name String
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name String
              specification GivenProjectOnItsOwnSource
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId Uuid
                name String
              query ProjectById => ProjectSummary?
                by projectId Uuid
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered
                  projectId = $eventSourceId
                  name = name
        """;

    SemanticSpecificationRun _run = null!;
    SemanticSpecificationRun _contextRun = null!;

    void Because()
    {
        _run = Run(Source);
        _contextRun = Run(Source.Replace("projectId = $eventSourceId", "projectId = $eventContext.eventSourceId", StringComparison.Ordinal));
    }

    [Fact] void should_establish_the_given_source_as_the_projection_key() => _run.Passed.ShouldBeTrue();
    [Fact] void should_read_the_event_context_source_explicitly() => _contextRun.Passed.ShouldBeTrue();
    [Fact] void should_carry_the_source_without_duplicating_it_in_event_payload() => _run.Execution.World.Facts.Single().Values.Length.ShouldEqual(1);

    SemanticSpecificationRun Run(string source)
    {
        var model = Bind(source).Value!.Model;
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        return new SemanticSpecificationRunner().Run(plan, plan.Specifications.Values.Single().Id);
    }
}
