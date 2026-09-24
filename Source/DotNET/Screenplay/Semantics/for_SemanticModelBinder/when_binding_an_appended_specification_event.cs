// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_appended_specification_event : given.a_semantic_binder
{
    const string Source =
        """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
              event ProjectRegistered
                projectId ProjectId
              specification AppendingWithSource
                when append ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
              specification AppendingWithoutSource
                when append ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _implicit;

    void Because()
    {
        _result = Bind(Source);
        _implicit = Bind(Source.Replace("for \"3fa85f64-5717-4562-b3fc-2c963f66afa6\"", string.Empty, StringComparison.Ordinal));
    }

    [Fact] void should_bind_both_actions() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_v2_for_the_explicit_event_source() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_select_v1_when_no_step_asserts_a_source() => _implicit.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V1);
    [Fact] void should_retain_the_typed_event_source() => Specifications.Single(spec => spec.Name == "AppendingWithSource").WhenAppended!.EventSource.ShouldNotBeNull();
    [Fact] void should_omit_the_source_on_the_other_action() => Specifications.Single(spec => spec.Name == "AppendingWithoutSource").WhenAppended!.EventSource.ShouldBeNull();
    [Fact] void should_not_invent_a_command_action() => Specifications.All(spec => spec.When is null).ShouldBeTrue();

    SemanticSpecification[] Specifications => [.. _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.OrderBy(spec => spec.Name)];
}
