// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_a_concept_with_attribute_reasons : given.a_compiler
{
    const string Source =
        """
        concept BankAccount : String @pii @sensitive
          pii reason "Payout account - lawful basis: contract performance"
          sensitive reason "Fraud sensitive - never rendered in full"

        concept EmailAddress : String @pii
          pii reason "Billing contact address"
          validate
            not empty  message "Email is required"

        concept InvoiceId : Uuid
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_both_attributes() => Concept("BankAccount").AttributeNames.ShouldContainOnly("pii", "sensitive");
    [Fact] void should_carry_the_pii_reason() => Attribute("BankAccount", "pii").Reason.ShouldEqual("Payout account - lawful basis: contract performance");
    [Fact] void should_carry_the_sensitive_reason() => Attribute("BankAccount", "sensitive").Reason.ShouldEqual("Fraud sensitive - never rendered in full");
    [Fact] void should_carry_a_reason_alongside_validation() => Attribute("EmailAddress", "pii").Reason.ShouldEqual("Billing contact address");
    [Fact] void should_keep_the_validation_rules() => Concept("EmailAddress").Validations!.OfType<DeclarativeValidateSyntax>().Single().Rules.Count().ShouldEqual(1);
    [Fact] void should_leave_an_unannotated_concept_bare() => Concept("InvoiceId").Attributes.ShouldBeEmpty();

    ConceptSyntax Concept(string name) => _result.Value!.Concepts.Single(_ => _.Name == name);

    ConceptAttributeSyntax Attribute(string concept, string attribute) => Concept(concept).Attributes.Single(_ => _.Name == attribute);
}
