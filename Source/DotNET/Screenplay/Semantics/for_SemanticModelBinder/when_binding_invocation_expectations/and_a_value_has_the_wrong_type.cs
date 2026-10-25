// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations;

public class and_a_value_has_the_wrong_type : given.an_invoice_closing_source
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(With(
        """
        then invokes CloseInvoice
          invoiceId = 42
        """));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
}
