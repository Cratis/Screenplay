// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_rule_implementation;

public class and_the_kind_is_not_a_named_predicate : given.a_printer
{
    const string Path = "Validations/ReasonIsPresent.cs";

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        given.a_hand_built_application.WithCommandRule(
            new ValidationRuleSyntax(
                given.a_hand_built_application.PropertyName,
                ValidationRuleKind.NotEmpty,
                null,
                "A reason is required",
                SourceLocation.Start,
                new FileReferenceSyntax(Path, SourceLocation.Start))));

    [Fact] void should_print_the_rule_itself() => _roundtrip.Printed.ShouldContain($"{given.a_hand_built_application.PropertyName} not empty message \"A reason is required\"");
    [Fact] void should_not_write_the_file_directive() => _roundtrip.Printed.Split('\n').Any(line => line.Trim() == $"file {Path}").ShouldBeFalse();
    [Fact] void should_note_the_omitted_file() => _roundtrip.Printed.ShouldContain($"// TODO: 'file {Path}' is not written here");
    [Fact] void should_say_why_it_is_not_written() => _roundtrip.Printed.ShouldContain("a rule implementation is read back only on a 'rule <Name>' predicate");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_rule_kind() => Rule.Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_preserve_the_message() => Rule.Message.ShouldEqual("A reason is required");
    [Fact] void should_reparse_without_the_file() => Rule.File.ShouldBeNull();
    [Fact] void should_have_nothing_left_to_note_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldNotContain("// TODO:");

    ValidationRuleSyntax Rule => given.a_hand_built_application.CommandRule(_roundtrip.Reparsed);
}
