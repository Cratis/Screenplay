// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_invalid_tag : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              event InvoiceRegistered
                tag
                tag ==
                invoiceId Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_not_succeed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_value() => _result.Diagnostics.First().Message.ShouldEqual("Expected a value after 'tag' - an identifier, a string literal or a context expression");
    [Fact] void should_report_the_invalid_value() => _result.Diagnostics.Last().Message.ShouldEqual("Invalid tag value '==' - expected an identifier, a string literal or a context expression");
    [Fact] void should_report_errors_only() => _result.Diagnostics.All(_ => _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_keep_the_event_property() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Events.Single().Properties.Single().Name.ShouldEqual("invoiceId");
}
