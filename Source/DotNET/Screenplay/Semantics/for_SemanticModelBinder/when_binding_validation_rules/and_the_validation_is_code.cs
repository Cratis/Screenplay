// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// Code validation stays rejected until a constrained implementation attachment exists (#139).
public class and_the_validation_is_code : given.a_semantic_binder
{
    const string Source =
        """
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              command PlaceOrder
                orderId Uuid identifier
                amount Decimal
                validate csharp
                  ```
                  if (context.Artifact.amount <= 0) yield return "Nothing to order";
                  ```
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_requires_an_implementation_attachment() => _result.Diagnostics.Single().Message.ShouldEqual("Command 'PlaceOrder' code validation requires a constrained implementation attachment (#139).");
}
