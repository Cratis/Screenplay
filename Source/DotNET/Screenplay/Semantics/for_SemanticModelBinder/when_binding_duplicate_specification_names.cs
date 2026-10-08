// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_duplicate_specification_names : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateChange S
              command C
              specification Repeated
                when C
              specification Repeated
                when C
        """);

    [Fact] void should_report_the_existing_duplicate_name_diagnostic() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("specification in slice", StringComparison.Ordinal));
    [Fact] void should_not_admit_duplicate_names() => _result.Success.ShouldBeFalse();
}
