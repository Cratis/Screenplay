// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_event_with_an_identifier : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId : Uuid

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                invoiceId InvoiceId identifier
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_identifier() => _result.Diagnostics.Single().Message.ShouldEqual("Property 'invoiceId' of event 'InvoiceRegistered' cannot be marked identifier - an event never carries its event source id");
    [Fact] void should_report_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_keep_the_property_unmarked() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties.Single().IsIdentifier.ShouldBeFalse();
}
