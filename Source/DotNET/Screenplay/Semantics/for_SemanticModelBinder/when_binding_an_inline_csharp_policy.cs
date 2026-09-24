// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

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

    [Fact] void should_block_implementation_until_139() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("#139", StringComparison.Ordinal)).ShouldBeTrue();
}
