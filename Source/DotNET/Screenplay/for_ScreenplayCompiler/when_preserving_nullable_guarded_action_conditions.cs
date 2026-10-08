// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

// Screen conditions are renderer contracts, not reference-runner expressions. Keep explicit null
// distinct from missing-field diagnostics instead of rewriting either to a truthy comparison.
public class when_preserving_nullable_guarded_action_conditions : Specification
{
    CompilationResult<ApplicationSyntax> _result;
    ScreenGuardedActionSyntax _action;

    void Because()
    {
        _result = new ScreenplayCompiler().Compile(
            """
            module M
              feature F
                slice StateView S
                  readmodel Item
                    status String optional
                  query View => Item
                  command Act
                  screen Details
                    data Item via query View
                    action "Choose"
                      when item.status != "closed" execute Act
                      when item.status == null execute Act
                      when item.missing == null execute Act
            """);
        _action = _result.Value!.Modules.Single().Features.Single().Slices.Single().Screens.Single().Directives.OfType<ScreenGuardedActionSyntax>().Single();
    }

    [Fact] void should_warn_only_on_the_unknown_field() => _result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.UnknownActionSubjectField);
    [Fact] void should_preserve_explicit_null_for_the_renderer() => ((LiteralExpressionSyntax)((ComparisonConditionSyntax)_action.Alternatives.ElementAt(1).Condition).Right).Value.ShouldBeNull();
    [Fact] void should_preserve_the_missing_path_without_inventing_a_value() => ((ComparisonConditionSyntax)_action.Alternatives.Last().Condition).Left.ShouldEqual("item.missing");
    [Fact] void should_preserve_the_inequality_operator() => ((ComparisonConditionSyntax)_action.Alternatives.First().Condition).Operator.ShouldEqual(ComparisonOperator.NotEqual);
}
