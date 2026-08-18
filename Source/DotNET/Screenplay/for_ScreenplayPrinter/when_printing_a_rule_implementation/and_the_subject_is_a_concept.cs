// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_rule_implementation;

public class and_the_subject_is_a_concept : given.a_printer
{
    const string Path = "Validations/NumberIsPresent.cs";

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        given.a_hand_built_application.WithConceptRule(
            new ValidationRuleSyntax(
                ValidationRuleSyntax.ConceptValue,
                ValidationRuleKind.NotEmpty,
                null,
                "An invoice number is required",
                SourceLocation.Start,
                new FileReferenceSyntax(Path, SourceLocation.Start))));

    [Fact] void should_print_the_rule_without_a_subject() => _roundtrip.Printed.ShouldContain("not empty message \"An invoice number is required\"");
    [Fact] void should_not_write_the_file_directive() => _roundtrip.Printed.Split('\n').Any(line => line.Trim() == $"file {Path}").ShouldBeFalse();
    [Fact] void should_note_the_omitted_file() => _roundtrip.Printed.ShouldContain($"// TODO: 'file {Path}' is not written here");
    [Fact] void should_reparse_successfully() => _roundtrip.Reparsed.Success.ShouldBeTrue();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_implied_subject() => Rule.Property.ShouldEqual(ValidationRuleSyntax.ConceptValue);
    [Fact] void should_preserve_the_rule_kind() => Rule.Rule.ShouldEqual(ValidationRuleKind.NotEmpty);
    [Fact] void should_reparse_without_the_file() => Rule.File.ShouldBeNull();
    [Fact] void should_have_nothing_left_to_note_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldNotContain("// TODO:");

    ValidationRuleSyntax Rule => given.a_hand_built_application.ConceptRule(_roundtrip.Reparsed);
}
