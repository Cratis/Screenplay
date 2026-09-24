// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_nested_artifact_path : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        type Details
          department String
        policy Access
          require claim "department" matches details.department
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                details Details
                authorize Access
        """);

    [Fact] void should_resolve_declared_composite_members() => _result.Success.ShouldBeTrue();
}
