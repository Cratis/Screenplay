// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_a_rule_implementation;

public class and_it_names_only_a_file : given.a_printer
{
    const string Path = "Validations/BeUnique.cs";

    const string Expected =
        """
                validate
                  orgNumber rule BeUnique
                    file Validations/BeUnique.cs
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(
        given.a_hand_built_application.WithCommandRule(
            new ValidationRuleSyntax(
                given.a_hand_built_application.PropertyName,
                ValidationRuleKind.Rule,
                new PathExpressionSyntax("BeUnique", SourceLocation.Start),
                null,
                SourceLocation.Start,
                new FileReferenceSyntax(Path, SourceLocation.Start))));

    [Fact] void should_print_the_block_unchanged() => _roundtrip.Printed.ShouldContain(Expected);
    [Fact] void should_note_nothing() => _roundtrip.Printed.ShouldNotContain("// TODO:");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_preserve_the_file() => Rule.File!.Path.ShouldEqual(Path);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    ValidationRuleSyntax Rule => given.a_hand_built_application.CommandRule(_roundtrip.Reparsed);
}
