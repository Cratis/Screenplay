// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_invocation_expectations;

public class and_a_property_is_unknown : given.an_invoice_closing_source
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(With(
        """
        then invokes CloseInvoice
          comment = "paid"
        """));

    [Fact] void should_fail() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unresolved_property() => _result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message == "The specification invocation property 'comment' is unresolved.");
}
