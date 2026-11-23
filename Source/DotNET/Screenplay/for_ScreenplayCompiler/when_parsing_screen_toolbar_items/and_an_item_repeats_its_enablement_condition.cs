// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler.when_parsing_screen_toolbar_items;

public class and_an_item_repeats_its_enablement_condition : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    ToolbarItemSyntax _item;
    Diagnostic[] _unknown;
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
                        enabled when item.status == "onSale"
                        enabled when item.status == "draft"
            """);
        _item = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single();
        _unknown = [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownScreenDirective)];
        _messages = [.. _unknown.Select(diagnostic => diagnostic.Message)];
    }

    [Fact] void should_keep_the_first_condition() => (_item.EnabledWhen!.Location.Line == _unknown[0].Location.Line - 1).ShouldBeTrue();
    [Fact] void should_report_two_unknown_directives() => _messages.Length.ShouldEqual(2);
    [Fact] void should_report_the_invalid_presentation_first() => _messages[0].StartsWith("Invalid presentation value", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_report_the_unexpected_child_second() => _messages[1].ShouldContain("in toolbar item - expected label, icon, confirm, enabled when, parameter or presentation");
}
