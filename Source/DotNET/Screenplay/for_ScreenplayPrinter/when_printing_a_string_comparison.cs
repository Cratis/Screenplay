// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// The word operators, in the one condition grammar every construct shares - so a reaction's <c>where</c>, a
/// command's <c>produces when</c> and a policy's <c>require</c> all read the same way. <c>starts with</c> is
/// two words because that is the phrase, which makes an operator something other than a single token for the
/// first time.
/// </summary>
public class when_printing_a_string_comparison : given.a_printer
{
    const string Source =
        """
        module Support
          feature Triage
            slice StateChange Triage
              event IssueOpened
                labels String
                title String

              command Triage
                labels String
                title String
                produces when labels contains "important" or title starts with "URGENT"
                  IssueOpened

            slice Automation Handle
              reaction HandleImportantIssue
                when IssueOpened
                  labels
                where labels contains "important"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_read_contains() => Left().Operator.ShouldEqual(ComparisonOperator.Contains);
    [Fact] void should_read_starts_with() => Right().Operator.ShouldEqual(ComparisonOperator.StartsWith);
    [Fact] void should_keep_the_operand_after_a_two_word_operator() =>
        ((LiteralExpressionSyntax)Right().Right).Value.ShouldEqual("URGENT");
    [Fact] void should_print_contains() => _roundtrip.Printed.ShouldContain("labels contains \"important\"");
    [Fact] void should_print_starts_with() => _roundtrip.Printed.ShouldContain("title starts with \"URGENT\"");

    [Fact] void should_read_it_in_a_reaction_condition() =>
        ((ComparisonConditionSyntax)Reaction.Where!).Operator.ShouldEqual(ComparisonOperator.Contains);

    LogicalConditionSyntax Condition =>
        (LogicalConditionSyntax)_roundtrip.Reparsed.Value!.Modules.Single().Features.Single()
            .Slices.First().Commands.Single().Produces.Single().When!;

    ComparisonConditionSyntax Left() => (ComparisonConditionSyntax)Condition.Left;

    ComparisonConditionSyntax Right() => (ComparisonConditionSyntax)Condition.Right;

    ReactionSyntax Reaction =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Last().Reactions.Single();
}
