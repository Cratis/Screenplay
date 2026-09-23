// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_measures_something_other_than_text : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _number;
    CompilationResult<SemanticCompilation> _enumeration;
    CompilationResult<SemanticCompilation> _textOperand;

    void Because()
    {
        _number = BindRules("quantity length == 3");
        _enumeration = BindRules("status length == 4");
        _textOperand = BindRules("name length == \"3\"");
    }

    [Fact] void should_report_a_number_as_unsupported_syntax() => _number.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_a_length_measures_text() => _number.Diagnostics.Single().Message.ShouldEqual("Validation rule 'length ==' on 'quantity' is not admitted: a length measures one text value that is not an enumeration.");
    [Fact] void should_report_an_enumeration_as_unsupported_syntax() => _enumeration.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_report_a_text_operand_as_an_invalid_binding() => _textOperand.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_say_the_operand_is_a_length() => _textOperand.Diagnostics.Single().Message.ShouldEqual("The 'length ==' operand on 'name' must be a non-negative whole number because it is a text length.");
}
