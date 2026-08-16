// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_resolving_a_contribution;

public class and_two_other_modules_declare_the_contribution_point : given.a_compiler
{
    // Payments has no template of its own, so its contribution bubbles out past its own module and finds
    // two equally-near candidates - Invoicing and Reporting - with nothing to say which one it means.
    const string Source =
        """
        module Invoicing
          screen template AppShell
            navbar contributes Navigation
            main

          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList

        module Reporting
          screen template ReportShell
            navbar contributes Navigation
            main

        module Payments
          contribute to Navigation
            navigate to InvoiceList
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed_with_a_warning() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_ambiguity() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.AmbiguousReference);
    [Fact] void should_report_it_as_a_warning() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Warning);
    [Fact] void should_name_both_candidate_modules() => _result.Diagnostics.Single().Message.ShouldContain("Invoicing, Reporting");
}
