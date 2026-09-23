// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

// These used to fall back to '==' and 'days' for any member without an arm; every member now has its own.
public class when_printing_every_comparison_operator_and_interval_unit : Specification
{
    string[] _comparisons;
    string[] _intervals;

    void Because()
    {
        _comparisons = [.. Enum.GetValues<ComparisonOperator>().Select(@operator =>
            ScreenplaySyntaxText.Condition(new ComparisonConditionSyntax("amount", @operator, new LiteralExpressionSyntax(1d, SourceLocation.Start), SourceLocation.Start)))];
        _intervals = [.. Enum.GetValues<IntervalUnit>().Select(unit =>
            ScreenplaySyntaxText.TriggerSource(new IntervalTriggerSourceSyntax(2, unit, SourceLocation.Start)))];
    }

    [Fact] void should_print_each_comparison_operator_distinctly() => _comparisons.Distinct().Count().ShouldEqual(Enum.GetValues<ComparisonOperator>().Length);
    [Fact] void should_print_each_interval_unit_distinctly() => _intervals.Distinct().Count().ShouldEqual(Enum.GetValues<IntervalUnit>().Length);
    [Fact] void should_print_days() => _intervals.ShouldContain("every 2 days");
}
