// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

public class when_printing_an_unknown_logical_operator : Specification
{
    Exception _error;

    void Because()
    {
        var operand = new ComparisonConditionSyntax("amount", ComparisonOperator.Equal, new LiteralExpressionSyntax(1d, SourceLocation.Start), SourceLocation.Start);
        _error = Catch.Exception(() => ScreenplaySyntaxText.Condition(new LogicalConditionSyntax(operand, (LogicalOperator)999, operand, SourceLocation.Start)));
    }

    [Fact] void should_refuse_to_print_it_rather_than_guess_or() => _error.ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
}
