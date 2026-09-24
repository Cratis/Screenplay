// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_unresolved_policy_target : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        policy Access
          require claim "department" matches unknown
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                department String
                authorize Access
        """);

    [Fact] void should_reject_the_unresolvable_artifact_path() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("unknown", StringComparison.Ordinal)).ShouldBeTrue();
}
