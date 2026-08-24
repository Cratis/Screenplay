// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_the_register_project_vertical : given.a_semantic_binder
{
    const string ProjectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6";
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
                validate
                  name not empty message "Project name is required"
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
                then query ProjectById
                  arguments
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  result
                    projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Screenplay"
              specification RejectingAnEmptyProjectName
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = ""
                then error "Project name is required"
            slice StateView ProjectLookup
              readmodel ProjectSummary
                projectId ProjectId
                name ProjectName
              query ProjectById => ProjectSummary?
                by projectId ProjectId
              projection ProjectSummaryProjection => ProjectSummary
                from ProjectRegistered key projectId
                  name = name
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_both_specifications() => ChangeSlice.Specifications.Length.ShouldEqual(2);
    [Fact] void should_bind_the_success_event() => Success.ThenEvents.Single().EventContract.ShouldEqual(ChangeSlice.Events.Single().Id);
    [Fact] void should_bind_the_success_read_model() => Success.ThenReadModels.Single().ReadModel.ShouldEqual(ViewSlice.ReadModels.Single().Id);
    [Fact] void should_bind_the_query_key() => ((SemanticTextValue)Success.ThenQueries.Single().Key).Value.ShouldEqual(ProjectId);
    [Fact] void should_bind_the_query_result() => Success.ThenQueries.Single().Results.Single().Values.Length.ShouldEqual(2);
    [Fact] void should_bind_the_rejection_message() => Rejection.ThenErrors.Single().Message.ShouldEqual("Project name is required");
    [Fact] void should_keep_the_rejection_free_of_success_outcomes() => (Rejection.ThenEvents.Length + Rejection.ThenReadModels.Length + Rejection.ThenQueries.Length).ShouldEqual(0);
    [Fact] void should_map_every_vertical_declaration_to_source() => _result.Value!.SourceMap.Entries.Length.ShouldEqual(21);

    SemanticSlice ChangeSlice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateChange);
    SemanticSpecification Rejection => ChangeSlice.Specifications.Single(_ => _.ThenErrors.Length > 0);
    SemanticSpecification Success => ChangeSlice.Specifications.Single(_ => _.ThenQueries.Length > 0);
    SemanticSlice ViewSlice => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Kind == SemanticSliceKind.StateView);
}
