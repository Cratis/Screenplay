// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_document : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId : Wat

        module Invoicing
          feature Invoices
            slice Wrong RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_unknown_primitive() => _result.Diagnostics.First().Message.ShouldEqual("Unknown primitive type 'Wat' - expected Uuid, String, Int, Decimal, Bool, Date, DateTime or Enum");
    [Fact] void should_report_the_unknown_slice_type() => _result.Diagnostics.Last().Message.ShouldEqual("Unknown slice type 'Wrong' - expected StateChange, StateView, Automation or Translate");
    [Fact] void should_report_the_slice_location() => _result.Diagnostics.Last().Location.ShouldEqual(new SourceLocation(5, 5));
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
}
