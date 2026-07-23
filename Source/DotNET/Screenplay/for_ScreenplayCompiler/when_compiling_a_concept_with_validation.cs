// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_concept_with_validation : given.a_compiler
{
    const string Source =
        """
        concept EmailAddress : String @pii
          validate
            not empty          message "Email is required"
            matches "^.+@.+$"  message "Must be a valid email address"
          validate csharp
            ```
            if (Value.EndsWith("@example.com")) yield ValidationError("Example addresses are not allowed");
            ```

        concept InvoiceStatus : Enum
          draft
          sent
          validate
            not empty  message "Status is required"
        """;

    CompilationResult<ApplicationSyntax> _result;
    ConceptSyntax _email;
    ConceptSyntax _status;

    void Because()
    {
        _result = _compiler.Compile(Source);
        _email = _result.Value!.Concepts.First();
        _status = _result.Value!.Concepts.Last();
    }

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_parse_the_declarative_rules() => EmailRules.Count().ShouldEqual(2);
    [Fact] void should_imply_the_concept_value_subject() => EmailRules.All(_ => _.Property == ValidationRuleSyntax.ConceptValue).ShouldBeTrue();
    [Fact] void should_parse_the_not_empty_rule() => EmailRules.First().Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_parse_the_matches_rule() => EmailRules.Last().Rule.ShouldEqual(ValidationRuleKind.Matches);
    [Fact] void should_parse_the_rule_messages() => EmailRules.First().Message.ShouldEqual("Email is required");
    [Fact] void should_parse_the_code_validation() => _email.Validations!.OfType<CodeValidateSyntax>().Single().Code.ShouldNotBeNull();
    [Fact] void should_keep_the_enum_values() => _status.Values.ShouldContainOnly("draft", "sent");
    [Fact] void should_parse_the_enum_concept_rules() => _status.Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Single().Rule.ShouldEqual(ValidationRuleKind.NotEmpty);

    IEnumerable<ValidationRuleSyntax> EmailRules => _email.Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules;
}
