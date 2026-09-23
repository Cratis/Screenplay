// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

public class when_printing_an_unknown_expression_kind : Specification
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplaySyntaxText.Expression(new an_unknown_expression(SourceLocation.Start)));

    [Fact] void should_refuse_to_print_it() => _error.ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
    [Fact] void should_name_the_kind() => _error.Message.ShouldContain(nameof(an_unknown_expression));

    // Nested and private, so the reflection over public syntax kinds elsewhere never sees it.
    sealed record an_unknown_expression(SourceLocation Location) : ExpressionSyntax(Location);
}
