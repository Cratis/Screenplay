// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

// A projection keyed on the event source identity - no key, as Chronicle defaults (ProjectionFactory.cs:1009-1017) - projects the
// fact a command produces for its destination. Given events carry no event source in ESM v1, so a specification that establishes
// such a projection from given events fails instead of guessing an identity.
public class when_running_a_specification_through_a_scoped_projection : Specification
{
    const string Source =
        """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              specification RegisteringAProject
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then readmodel ProjectSummary
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                  registrations = 1
              specification RegisteringAgain
                given ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "Screenplay"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
                registrations Int?
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered
                  name = name
                  count registrations
        """;

    SemanticExecutionPlan _plan;
    SemanticSpecificationRun _registering;
    SemanticSpecificationRun _registeringAgain;

    void Establish()
    {
        const string StableKey = "scoped-projection";
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "Projects.play", Source);
        var compilation = new SemanticModelCompiler().Compile("Projects", SemanticDocumentSet.Create([document], catalog));
        _plan = SemanticExecutionPlan.Compile(compilation.Value!.Model).Plan!;
    }

    void Because()
    {
        var runner = new SemanticSpecificationRunner();
        _registering = runner.Run(_plan, _plan.Specifications.Values.Single(_ => _.Name == "RegisteringAProject").Id);
        _registeringAgain = runner.Run(_plan, _plan.Specifications.Values.Single(_ => _.Name == "RegisteringAgain").Id);
    }

    [Fact] void should_project_the_command_fact_through_the_scope() => _registering.Passed.ShouldBeTrue();
    [Fact] void should_not_guess_an_event_source_for_given_events() => _registeringAgain.Passed.ShouldBeFalse();
    [Fact] void should_say_why() => _registeringAgain.Failures.Single().ShouldContain("event source identity");
}
