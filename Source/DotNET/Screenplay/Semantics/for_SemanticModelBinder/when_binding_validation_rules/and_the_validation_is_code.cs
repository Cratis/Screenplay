// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// Whole-command code validation binds as an opaque v3 attachment.
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

    [Fact] void should_bind_v3() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V3);
    [Fact] void should_reference_the_attachment() => _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().CodeValidations.Single().RequirementId.ShouldEqual(_result.ImplementationRequirements.Single().RequirementId);
    [Fact] void should_require_pure_capability() => _result.ImplementationRequirements.Single().RequiredCapability.ShouldEqual("pure");
}
