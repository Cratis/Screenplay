// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Printing.for_ScreenplaySyntaxText;

// Every declared kind must have its own printer arm - a kind without one used to print only its operand.
public class when_printing_every_validation_rule_kind : Specification
{
    ValidationRuleSyntax[] _rules;
    Exception? _error;
    string[] _printed = [];

    void Establish() => _rules = [.. Enum.GetValues<ValidationRuleKind>()
        .Select(kind => new ValidationRuleSyntax("amount", kind, new LiteralExpressionSyntax(42d, SourceLocation.Start), null, SourceLocation.Start))];

    void Because() => _error = Catch.Exception(() => _printed = [.. _rules.Select(ScreenplaySyntaxText.ValidationRule)]);

    [Fact] void should_print_every_kind() => _error.ShouldBeNull();
    [Fact] void should_print_a_keyword_or_operator_for_every_operand_kind() =>
        _printed.Where((_, index) => _rules[index].Rule != ValidationRuleKind.NotEmpty).All(text => text != "amount 42").ShouldBeTrue();
    [Fact] void should_print_each_kind_distinctly() => _printed.Distinct().Count().ShouldEqual(_rules.Length);
}
