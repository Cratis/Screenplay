// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_structural_syntax : given.a_semantic_binder
{
    const string Source =
        """
        concept ProjectId : Uuid
        concept ProjectStatus : Enum
          active
          archived
        type ProjectDetails
          projectId ProjectId
          status ProjectStatus
        module Projects
          feature Registration
            slice StateChange RegisterProject
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_bind_successfully() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_bind_the_application_name() => _result.Value!.Model.Application.Name.ShouldEqual("Projects");
    [Fact] void should_bind_the_concepts() => _result.Value!.Model.Application.Concepts.Length.ShouldEqual(2);
    [Fact] void should_bind_the_enumeration_values() => _result.Value!.Model.Application.Concepts.Single(_ => _.Name == "ProjectStatus").Values.ShouldContainOnly("active", "archived");
    [Fact] void should_bind_the_composite_type() => _result.Value!.Model.Application.Types.Single().Properties.Length.ShouldEqual(2);
    [Fact] void should_bind_the_module_feature_and_slice() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Name.ShouldEqual("RegisterProject");
    [Fact] void should_map_every_bound_declaration_to_source() => _result.Value!.SourceMap.Entries.Length.ShouldEqual(9);
}
