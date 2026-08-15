// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_document_with_duplicate_forms : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId Uuid

          form RegisterInvoiceForm for RegisterInvoice
            field invoiceId

          form RegisterInvoiceForm for RegisterInvoice
            field invoiceId
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_duplicate_form() => _result.Diagnostics.Single().Message.ShouldEqual("Duplicate form 'RegisterInvoiceForm' - a form is declared once");
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_only_the_first_occurrence() => _result.Value!.Modules.Single().Forms!.Count().ShouldEqual(1);
}
