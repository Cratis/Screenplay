// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_subjects_on_non_action_toolbar_items : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    ToolbarItemSyntax[] _items;

    void Because()
    {
        _result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  screen Details
                    toolbar
                      item action navigate to Details for item
                      item go navigate to action for screen
                      item open dialog action for item
            """);
        _items = [.. _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenToolbarSyntax>().Single().Items];
    }

    [Fact] void should_reject_each_inapplicable_subject_clause() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidActionSubjectClause).ShouldEqual(3);
    [Fact] void should_not_attach_subjects_to_non_action_items() => _items.All(item => item.Subject is null).ShouldBeTrue();
    [Fact] void should_preserve_the_navigation_named_action() => _items[1].Target.ShouldEqual("action");
    [Fact] void should_preserve_the_item_named_action() => _items[0].Name.ShouldEqual("action");
    [Fact] void should_preserve_the_dialog_named_action() => _items[2].Target.ShouldEqual("action");
}
