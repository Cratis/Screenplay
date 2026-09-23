// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_a_nested_property_path : given.a_semantic_binder
{
    const string Source =
        """
        type Address
          street String
        module Customers
          feature Registration
            slice StateChange RegisterCustomer
              event CustomerRegistered
                address Address
              constraint OneCustomerPerStreet
                unique address.street on CustomerRegistered
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_keep_the_unsupported_semantic_syntax_code() => Rejection.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_the_path_is_not_admitted() => Rejection.Message.ShouldContain("property path 'address.street' is not admitted by ESM v1");

    Diagnostic Rejection => _result.Diagnostics.Single(_ => _.Message.StartsWith("Constraint 'OneCustomerPerStreet'", StringComparison.Ordinal));
}
