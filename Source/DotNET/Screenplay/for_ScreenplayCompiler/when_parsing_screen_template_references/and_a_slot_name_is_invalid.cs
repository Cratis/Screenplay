// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_template_references;

public class and_a_slot_name_is_invalid : given.a_compiler
{
    ScreenTemplateReferenceSyntax _reference;
    Diagnostic[] _slotErrors;

    void Because()
    {
        var result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    template Shell
                      Bad
                        title "Skipped"
                      main
                        title "Kept"
            """);
        _reference = result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenTemplateReferenceSyntax>().Single();
        _slotErrors = [.. result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidScreenLayoutSlot)];
    }

    [Fact] void should_report_one_invalid_slot() => _slotErrors.Length.ShouldEqual(1);
    [Fact] void should_report_the_exact_message() => _slotErrors[0].Message.ShouldEqual("Expected a slot name in template 'Shell', got 'Bad'");
    [Fact] void should_add_only_the_valid_slot() => _reference.Slots.Single().Name.ShouldEqual("main");
    [Fact] void should_keep_the_directives_of_the_valid_slot() => _reference.Slots.Single().Directives.OfType<ScreenTitleSyntax>().Single().Text.ShouldEqual("Kept");
}
