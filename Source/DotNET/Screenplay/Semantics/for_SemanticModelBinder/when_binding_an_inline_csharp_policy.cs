// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_inline_csharp_policy : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        policy Access
          ```csharp
          return true;
          ```
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Access
        """);

    [Fact] void should_bind_as_an_opaque_v3_predicate() => _result.Value!.Model.Application.Policies.Single().Condition.ShouldBeOfExactType<SemanticOpaquePolicyCondition>();
    [Fact] void should_require_a_pure_predicate() => _result.ImplementationRequirements.Single().RequiredCapability.ShouldEqual("pure");
}
