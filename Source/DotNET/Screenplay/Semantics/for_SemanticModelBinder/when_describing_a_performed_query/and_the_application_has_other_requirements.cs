// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_application_has_other_requirements : for_SemanticModelBinder.given.a_semantic_binder
{
    const string Source =
        """
        policy Access
          ```csharp
          return true;
          ```
        module Orders
          feature Ordering
            slice StateChange PlaceOrder
              event OrderPlaced
                orderId Uuid
              command PlaceOrder
                orderId Uuid identifier
                handler
                  file Handlers/PlaceOrder.cs
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_list_requirements() => _result.ImplementationRequirements.ShouldNotBeEmpty();
    [Fact] void should_carry_no_query_shape() => _result.ImplementationRequirements.ShouldEachConformTo(_ => _.QueryShape == null);
}
