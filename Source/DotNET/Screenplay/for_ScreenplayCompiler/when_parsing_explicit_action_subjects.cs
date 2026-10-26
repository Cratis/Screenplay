// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_parsing_explicit_action_subjects : given.a_compiler
{
    CompilationResult<ApplicationSyntax> _result;
    ScreenSyntax _screen;

    void Because()
    {
        _result = _compiler.Compile(
            """
            module Work
              feature Items
                slice StateView Details
                  command Retry
                    itemId Uuid
                  query ExternalDetails => ExternalItem
                  screen Details
                    data ExternalItem via query ExternalDetails
                    action Retry for item
                      with itemId from item.itemId
                    action Work.Items.Details.Retry for screen
                    action "Again" for screen
                      when item.ready == true execute Retry
                    toolbar
                      item retry action Retry for item
                        parameter itemId from item.itemId
            """);
        _screen = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single();
    }

    [Fact] void should_accept_the_explicit_subjects() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_the_item_subject() => _screen.Directives.OfType<ScreenActionSyntax>().First().Subject.ShouldEqual(ScreenActionSubjectKind.Item);
    [Fact] void should_keep_the_screen_subject() => _screen.Directives.OfType<ScreenActionSyntax>().Last().Subject.ShouldEqual(ScreenActionSubjectKind.Screen);
    [Fact] void should_keep_the_explicit_input() => _screen.Directives.OfType<ScreenActionSyntax>().First().Inputs.Single().Binding.ShouldEqual("item.itemId");
    [Fact] void should_keep_the_guarded_subject() => _screen.Directives.OfType<ScreenGuardedActionSyntax>().Single().Subject.ShouldEqual(ScreenActionSubjectKind.Screen);
    [Fact] void should_keep_the_toolbar_subject() => _screen.Directives.OfType<ScreenToolbarSyntax>().Single().Items.Single().Subject.ShouldEqual(ScreenActionSubjectKind.Item);
}
