// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

public class when_printing_an_unknown_comparison_operator : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplaySyntaxText.Condition(
        new ComparisonConditionSyntax("amount", (ComparisonOperator)999, new LiteralExpressionSyntax(1d, SourceLocation.Start), SourceLocation.Start)));

    [Fact] void should_refuse_to_print_it_rather_than_guess_equality() => _error.ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
}
