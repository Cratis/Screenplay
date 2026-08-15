// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_form_with_an_unknown_field_property : given.a_compiler
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
            field customerName
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_unknown_field_property() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnknownFormFieldProperty);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_the_field_and_the_command() =>
        _result.Diagnostics.Single().Message.ShouldEqual("Form 'RegisterInvoiceForm' has a field for 'customerName', which is not a property of command 'RegisterInvoice'");
}
