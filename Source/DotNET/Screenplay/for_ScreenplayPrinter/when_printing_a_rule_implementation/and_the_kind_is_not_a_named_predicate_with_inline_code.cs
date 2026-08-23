// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_rule_implementation;

public class and_the_kind_is_not_a_named_predicate_with_inline_code : given.a_printer
{
    const string Code = "return !string.IsNullOrEmpty(value);";

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        given.a_hand_built_application.WithCommandRule(
            new ValidationRuleSyntax(
                given.a_hand_built_application.PropertyName,
                ValidationRuleKind.NotEmpty,
                null,
                null,
                SourceLocation.Start,
                Code: new CodeBlockSyntax("csharp", Code, SourceLocation.Start))));

    [Fact] void should_print_the_rule_itself() => _roundtrip.Printed.ShouldContain($"{given.a_hand_built_application.PropertyName} not empty");
    [Fact] void should_not_write_a_fence() => _roundtrip.Printed.ShouldNotContain("```");
    [Fact] void should_not_write_the_code() => _roundtrip.Printed.ShouldNotContain(Code);
    [Fact] void should_note_the_omitted_block() => _roundtrip.Printed.ShouldContain("// TODO: the inline 'csharp' block is not written here");
    [Fact] void should_say_why_it_is_not_written() => _roundtrip.Printed.ShouldContain("a rule implementation is read back only on a 'rule <Name>' predicate");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_rule_kind() => Rule.Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_reparse_without_the_code() => Rule.Code.ShouldBeNull();
    [Fact] void should_have_nothing_left_to_note_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldNotContain("// TODO:");

    ValidationRuleSyntax Rule => given.a_hand_built_application.CommandRule(_roundtrip.Reparsed);
}
