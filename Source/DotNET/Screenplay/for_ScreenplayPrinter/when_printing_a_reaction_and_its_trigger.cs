// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

/// <summary>
/// Every trigger form through the printer and back, because a schedule is the one thing a document can now
/// say that it could not before - and a form that prints back differently than it was written is a form the
/// round trip loses.
/// </summary>
public class when_printing_a_reaction_and_its_trigger : given.a_printer
{
    const string Source =
        """
        trigger DirectoryChanged
          description "The external directory published a change"
          entry BillingContact
          changedAt

        module Billing
          feature Directory
            slice Automation Sync
              reaction DirectorySync
                description "Reconciles the contact list against the directory"
                every 15 minutes
                at 08:00
                at 09:30 on Monday
                at 00:00 on day 1
                every 1 day
                when DirectoryChanged
                  entry
                  changedAt
                where entry.status == "active"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    [Fact] void should_print_the_trigger_declaration() => _roundtrip.Printed.ShouldContain("trigger DirectoryChanged");
    [Fact] void should_print_a_typed_trigger_value() => _roundtrip.Printed.ShouldContain("entry BillingContact");
    [Fact] void should_print_an_untyped_trigger_value_without_inventing_a_type() => _roundtrip.Printed.ShouldContain("\n  changedAt\n");

    [Fact] void should_print_the_interval() => _roundtrip.Printed.ShouldContain("every 15 minutes");
    [Fact] void should_print_a_singular_interval_in_the_singular() => _roundtrip.Printed.ShouldContain("every 1 day");
    [Fact] void should_print_the_daily_schedule() => _roundtrip.Printed.ShouldContain("at 08:00");
    [Fact] void should_print_the_weekly_schedule() => _roundtrip.Printed.ShouldContain("at 09:30 on Monday");
    [Fact] void should_print_the_monthly_schedule() => _roundtrip.Printed.ShouldContain("at 00:00 on day 1");
    [Fact] void should_print_the_named_trigger() => _roundtrip.Printed.ShouldContain("when DirectoryChanged");
    [Fact] void should_print_the_condition() => _roundtrip.Printed.ShouldContain("where entry.status == \"active\"");

    [Fact] void should_keep_every_trigger() => Reaction.Triggers.Count().ShouldEqual(6);
    [Fact] void should_keep_the_values_the_reaction_takes() => Reaction.Triggers.Last().Data.Select(_ => _.Name).ShouldContainOnly("entry", "changedAt");

    ReactionSyntax Reaction =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Single().Reactions.Single();
}
