// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_quantifies_over_something_else : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _scalar;
    CompilationResult<SemanticCompilation> _textCollection;

    void Because()
    {
        _scalar = BindRules("amount all > 0");
        _textCollection = BindRules("tags all >= 0");
    }

    [Fact] void should_report_a_scalar_as_unsupported_syntax() => _scalar.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_all_quantifies_over_numbers() => _scalar.Diagnostics.Single().Message.ShouldEqual("Validation rule 'all >' on 'amount' is not admitted: 'all' quantifies over a collection of whole or decimal numbers.");
    [Fact] void should_report_a_text_collection_as_unsupported_syntax() => _textCollection.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
}
