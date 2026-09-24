// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_specification_event_source_syntax : given.a_semantic_binder
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
              event ProjectRegistered
              specification ExplicitEventSources
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when RegisterProject
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_the_explicit_sources() => _result.Success.ShouldBeTrue();
    [Fact] void should_choose_the_v2_pair() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
    [Fact] void should_bind_the_given_source() => Specification.GivenEvents.Single().EventSource.ShouldNotBeNull();
    [Fact] void should_bind_the_command_source() => Specification.When!.EventSource.ShouldNotBeNull();
    [Fact] void should_bind_the_expected_source() => Specification.ThenEvents.Single().EventSource.ShouldNotBeNull();

    SemanticSpecification Specification => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single();
}
