// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_dialog_template_that_fits_a_slot : given.a_compiler
{
    const string Source =
        """
        module Invoicing
          dialog template RegisterInvoiceDialog
            fits slot content

            body
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_report_that_a_dialog_fills_no_slot() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.FitsSlotNotAllowed);
    [Fact] void should_report_it_as_an_error() => _result.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_still_parse_the_slots() => _result.Value!.Modules.Single().DialogTemplates!.Single().Slots.Select(slot => slot.Name).ShouldContainOnly("body");
}
