// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayCompiler;

public class when_compiling_concept_attributes_with_reasons : given.a_compiler
{
    const string Source =
        """
        concept BankAccountNumber : String @pii("Partner payout account - lawful basis: \"contract performance\".") @sensitive
        concept EmailAddress : String @pii
        concept CustomerRank : Int @ranked("Ordering hint")
        """;

    CompilationResult<ApplicationSyntax> _result;

    void Because() => _result = _compiler.Compile(Source);

    [Fact] void should_succeed() => _result.Success.ShouldBeTrue();
    [Fact] void should_have_no_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_carry_the_reason() => Attribute("BankAccountNumber", "pii").Value.ShouldEqual("Partner payout account - lawful basis: \"contract performance\".");
    [Fact] void should_keep_an_argumentless_attribute_alongside() => Attribute("BankAccountNumber", "sensitive").Value.ShouldBeNull();
    [Fact] void should_keep_a_bare_attribute_valid() => Attribute("EmailAddress", "pii").Value.ShouldBeNull();
    [Fact] void should_tolerate_an_unknown_attribute_with_an_argument() => Attribute("CustomerRank", "ranked").Value.ShouldEqual("Ordering hint");

    ConceptAttributeSyntax Attribute(string concept, string name) =>
        _result.Value!.Concepts.Single(_ => _.Name == concept).Attributes.Single(_ => _.Name == name);
}
