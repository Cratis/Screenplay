// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_an_interaction_names_things_that_do_not_exist : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          feature InvoiceManagement
            slice StateView InvoiceList
              screen InvoiceList
                on enter
                  refresh ThereIsNoSuchQuery

                on click
                  execute ThereIsNoSuchCommand
                    on success
                      navigate to ThereIsNoSuchScreen

                uses ThereIsNoSuchBehavior
        """;

    CompilationResult<Syntax.ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_the_unknown_query() => CodeAt(DiagnosticCodes.UnknownActionQuery).ShouldNotBeNull();
    [Fact] void should_report_the_unknown_command() => CodeAt(DiagnosticCodes.UnknownActionCommand).ShouldNotBeNull();
    [Fact] void should_report_the_unknown_screen() => CodeAt(DiagnosticCodes.UnknownActionScreen).ShouldNotBeNull();
    [Fact] void should_report_the_unknown_behavior() => CodeAt(DiagnosticCodes.UnknownUsedBehavior).ShouldNotBeNull();

    [Fact]
    void should_name_what_did_not_resolve() =>
        CodeAt(DiagnosticCodes.UnknownActionCommand)!.Message.ShouldContain("ThereIsNoSuchCommand");

    // An unresolved reference stays a warning, the way every other reference in the document does - a name may
    // still resolve to something outside it. What must never happen is silence.
    [Fact]
    void should_report_them_as_warnings_rather_than_blocking_the_compilation() =>
        _result.Diagnostics
            .Where(diagnostic => diagnostic.Code.StartsWith("PLAY033", StringComparison.Ordinal))
            .Select(diagnostic => diagnostic.Severity)
            .Distinct()
            .ShouldContainOnly(DiagnosticSeverity.Warning);

    Diagnostic? CodeAt(string code) => _result.Diagnostics.FirstOrDefault(diagnostic => diagnostic.Code == code);
}
