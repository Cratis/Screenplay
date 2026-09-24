// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_an_authorized_specification_without_a_caller : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(
        """
        policy Access
          require authenticated
        module Portal
          feature Reports
            slice StateChange FileReport
              command FileReport
                authorize Access
              specification MissingCaller
                when FileReport
                then denied
        """);

    [Fact] void should_report_missing_identity_context() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.MissingSpecificationCaller).ShouldBeTrue();
}
