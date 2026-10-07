// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_rejecting_malformed_guarded_actions
{
    [Theory]
    [InlineData("when item.status == \"open\"", DiagnosticCodes.InvalidActionAlternative)]
    [InlineData("otherwise disabled", DiagnosticCodes.InvalidActionAlternative)]
    [InlineData("otherwise hidden", DiagnosticCodes.GuardedActionWithoutAlternatives)]
    [InlineData("when item.status == \"open\" execute Retry\notherwise hidden\notherwise hidden", DiagnosticCodes.MisplacedActionOtherwise)]
    [InlineData("otherwise hidden\nwhen item.status == \"open\" execute Retry", DiagnosticCodes.MisplacedActionOtherwise)]
    [InlineData("when status == \"open\" execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status == item.other execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status == $env.status execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status > \"open\" execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status contains 42 execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status starts with null execute Retry", DiagnosticCodes.UnsupportedActionConditionOperand)]
    [InlineData("when item.status == \"open\" execute Retry\nlabel \"Again\"", DiagnosticCodes.UnknownActionDirective)]
    [InlineData("when item.status execute Retry", DiagnosticCodes.ExpectedComparisonOperator)]
    [InlineData("when (item.status == \"open\" execute Retry", DiagnosticCodes.UnclosedConditionGroup)]
    [InlineData("when item.status == \"open\" execute Retry\notherwise hidden\n  with id from item.id", DiagnosticCodes.InvalidActionAlternative)]
    [InlineData("when item.status == \"open\" execute Retry\n  with id", DiagnosticCodes.InvalidInteractionArgument)]
    [InlineData("when item.status == \"open\" execute Retry\nnavigate to Details\nnavigate to Details", DiagnosticCodes.InvalidActionAlternative)]
    public void should_report_the_structural_error(string body, string code)
    {
        var source = "module Work\n  feature Items\n    slice StateView Details\n      command Retry\n      screen Details\n        action \"Again\"\n          " + body.Replace("\n", "\n          ", StringComparison.Ordinal);
        var result = new ScreenplayCompiler().Compile(source);
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == code && diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    }
}
