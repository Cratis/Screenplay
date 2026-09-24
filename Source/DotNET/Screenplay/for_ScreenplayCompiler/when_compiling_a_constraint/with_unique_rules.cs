// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_compiling_a_constraint;

public class with_unique_rules : given.a_compiler
{
    const string Source =
        """
        module Billing
          feature Invoices
            slice StateChange IssueInvoice
              event InvoiceIssued
                invoiceNumber String
              constraint UniqueInvoiceNumber
                unique invoiceNumber on InvoiceIssued
              constraint OneIssuePerInvoice
                unique event InvoiceIssued
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_compile_both_unique_forms() => _result.Success.ShouldBeTrue();
    [Fact] void should_not_warn_about_the_unique_forms() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_retain_both_rules() => _result.Value!.Modules.Single().Features.Single().Slices.Single().Constraints.Count().ShouldEqual(2);
}
