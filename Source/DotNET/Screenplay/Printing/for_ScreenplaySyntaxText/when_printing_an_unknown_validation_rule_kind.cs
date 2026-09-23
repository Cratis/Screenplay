// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

public class when_printing_an_unknown_validation_rule_kind : Specification
{
    const ValidationRuleKind UnknownKind = (ValidationRuleKind)999;
    Exception _error;

    void Because() => _error = Catch.Exception(() => ScreenplaySyntaxText.ValidationRule(
        new ValidationRuleSyntax("amount", UnknownKind, new LiteralExpressionSyntax(42d, SourceLocation.Start), null, SourceLocation.Start)));

    [Fact] void should_refuse_to_print_it() => _error.ShouldBeOfExactType<UnsupportedSyntaxForPrinting>();
    [Fact] void should_name_the_kind() => _error.Message.ShouldContain("999");
}
