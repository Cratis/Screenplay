// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_toolbar_items;

public class and_an_item_has_an_unknown_child : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    string[] _messages;

    void Because()
    {
        _result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    toolbar
                      item open navigate to Details
                        sparkle "now"
            """);
        _messages = [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreenDirective).Select(diagnostic => diagnostic.Message)];
    }

    [Fact] void should_report_two_unknown_directives() => _messages.Length.ShouldEqual(2);
    [Fact] void should_report_the_invalid_presentation_first() => _messages[0].ShouldEqual("Invalid presentation value 'sparkle \"now\"' - expected 'presentation <key> \"value\"'");
    [Fact] void should_report_the_unexpected_child_second() => _messages[1].ShouldEqual("Unexpected 'sparkle \"now\"' in toolbar item - expected label, icon, confirm, enabled when, parameter or presentation");
}
