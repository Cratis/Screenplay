// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_guarded_screen_action : given.a_compiler
{
    const string Source =
        """
        module Work
          feature Items
            slice StateView Details
              readmodel Item
                itemId Uuid
                status String
                answered Boolean
              query ItemDetails => Item
              command Retry
                itemId Uuid
              command Release
                itemId Uuid
              screen Details
                data Item via query ItemDetails
                section actions
                  action "Read the source again"
                    when item.status == "open execute Retry" and item.answered == false execute Release
                      with itemId from item.itemId
                    when item.status == "failed" execute Retry
                    otherwise execute Release
                      with itemId from item.itemId
                    navigate to Details
        """;

    CompilationResult<ApplicationSyntax> _result;
    ScreenGuardedActionSyntax _action;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _action = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenSectionSyntax>().Single().Directives.OfType<ScreenGuardedActionSyntax>().Single();
    }

    [Fact] void should_compile_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_one_label() => _action.Label.ShouldEqual("Read the source again");
    [Fact] void should_preserve_authored_command_order() => _action.Alternatives.Select(alternative => alternative.Command).ShouldContainOnly("Release", "Retry");
    [Fact] void should_not_split_on_execute_inside_a_literal() => ((LiteralExpressionSyntax)((ComparisonConditionSyntax)((LogicalConditionSyntax)_action.Alternatives.First().Condition).Left).Right).Value.ShouldEqual("open execute Retry");
    [Fact] void should_keep_the_alternative_arguments() => _action.Alternatives.First().Arguments.Single().Binding.ShouldEqual("item.itemId");
    [Fact] void should_keep_the_fallback_command() => _action.Otherwise!.Command.ShouldEqual("Release");
    [Fact] void should_keep_the_fallback_arguments() => _action.Otherwise!.Arguments.Single().Name.ShouldEqual("itemId");
    [Fact] void should_keep_post_success_navigation() => _action.Navigate!.Screen.ShouldEqual("Details");
}
