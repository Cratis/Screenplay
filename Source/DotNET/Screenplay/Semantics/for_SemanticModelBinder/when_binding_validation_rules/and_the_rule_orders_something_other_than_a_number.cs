// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_orders_something_other_than_a_number : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _text;
    CompilationResult<SemanticCompilation> _date;
    CompilationResult<SemanticCompilation> _today;

    void Because()
    {
        _text = BindRules("name > \"a\"");
        _date = BindRules("dueDate < today");
        _today = BindRules("amount > today");
    }

    [Fact] void should_report_text_as_unsupported_syntax() => _text.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_ordering_compares_numbers() => _text.Diagnostics.Single().Message.ShouldEqual("Validation rule '>' on 'name' is not admitted: ordering compares one whole or decimal number.");
    [Fact] void should_say_a_date_cannot_be_compared() => _date.Diagnostics.Single().Message.ShouldEqual("Validation rule '<' on 'dueDate' is not admitted: ESM v1 has no runtime date value - dates are text in a fixed format, so they cannot be compared.");
    [Fact] void should_report_today_as_unsupported_syntax() => _today.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_today_has_no_value() => _today.Diagnostics.Single().Message.ShouldEqual("Validation rule '>' on 'amount' is not admitted: 'today' has no runtime value because ESM v1 has no runtime date value - dates are text in a fixed format, so they cannot be compared.");
}
