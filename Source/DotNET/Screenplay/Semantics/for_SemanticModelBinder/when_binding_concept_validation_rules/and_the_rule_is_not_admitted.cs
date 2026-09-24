// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_concept_validation_rules;

public class and_the_rule_is_not_admitted : given.a_semantic_binder
{
    const string Source =
        """
        concept EmailAddress : String
          validate
            matches email message "Not an email address"
            require value != ""
        concept Birthday : Date
          validate
            == "2000-01-01"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_every_rule_as_unsupported_syntax() => _result.Diagnostics.All(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeTrue();
    [Fact] void should_say_concept_requirements_are_not_admitted() => Messages.ShouldContain("Concept 'EmailAddress' require is not admitted: requirements guard commands, not concept values; declare a concept validation rule instead.");
    [Fact] void should_say_a_date_cannot_be_compared() => Messages.ShouldContain("Validation rule '==' on concept 'Birthday' is not admitted: ESM v1 has no runtime date value - dates are text in a fixed format, so they cannot be compared.");

    IEnumerable<string> Messages => _result.Diagnostics.Select(_ => _.Message);
}
