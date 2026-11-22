// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_toolbar_items;

public class and_an_item_header_is_invalid : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    ScreenToolbarSyntax _toolbar;
    Diagnostic[] _unknown;

    void Because()
    {
        _result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    toolbar
                      item 1bad navigate to Details
                        label "Open"
                        sparkle
            """);
        _toolbar = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single();
        _unknown = [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreenDirective)];
    }

    [Fact] void should_report_one_unknown_directive() => _unknown.Length.ShouldEqual(1);
    [Fact] void should_report_the_invalid_toolbar_item() => _unknown[0].Message.StartsWith("Invalid toolbar item 'item 1bad navigate to Details'", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_add_the_item() => _toolbar.Items.ShouldBeEmpty();
}
