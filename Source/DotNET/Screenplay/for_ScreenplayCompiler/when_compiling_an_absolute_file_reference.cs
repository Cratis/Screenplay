// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_an_absolute_file_reference : given.a_compiler
{
    const string Source =
        """
        concept InvoiceId : Uuid
          file /Users/someone/Invoicing/InvoiceId.cs

        module Invoicing
          feature Invoices
            slice StateChange RegisterInvoice
              command RegisterInvoice
                invoiceId InvoiceId identifier
                handler
                  file Invoicing/RegisterInvoice/RegisterInvoice.cs
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_still_compile() => _result.Success.ShouldBeTrue();
    [Fact] void should_warn_about_the_absolute_path() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AbsoluteFileReference);
    [Fact] void should_warn_rather_than_reject() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_point_at_the_line_it_is_on() => _result.Diagnostics.Single().Location.Line.ShouldEqual(2);
    [Fact] void should_carry_the_path_it_was_given() => _result.Value!.Concepts.Single().File!.Path.ShouldEqual("/Users/someone/Invoicing/InvoiceId.cs");
    [Fact] void should_leave_the_relative_one_alone() => _result.Diagnostics.Count(_ => _.Code == DiagnosticCodes.AbsoluteFileReference).ShouldEqual(1);
}
