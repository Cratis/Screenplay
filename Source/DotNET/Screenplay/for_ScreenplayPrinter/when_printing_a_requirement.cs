// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_requirement : given.a_printer
{
    // The rule as it was ruled on in #81 - one comparison against read-model state, combined with 'and',
    // and the message in the body rather than on the end of the line.
    const string Source =
        """
        module Timesheets
          feature HourRegistration
            slice StateChange StartMonth
              command StartMonth
                engagementId Uuid
                reads EngagementScope by engagementId

                validate
                  require EngagementScope.isStarted == false and EngagementScope.phase == "Contract"
                    message "The month is already started"

                produces TimesheetStarted
                  engagementId = engagementId

              event TimesheetStarted
                engagementId Uuid

            slice StateView Scopes
              projection Scopes => EngagementScope
                from TimesheetStarted key engagementId
                  isStarted = engagementId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_condition() =>
        _roundtrip.Printed.ShouldContain(@"require EngagementScope.isStarted == false and EngagementScope.phase == ""Contract""");
    [Fact] void should_print_the_message_in_the_body() =>
        _roundtrip.Printed.ShouldContain(@"message ""The month is already started""");
    [Fact] void should_keep_the_message() => Requirement.Message.ShouldEqual("The month is already started");

    // The condition is the language's one condition grammar, so it combines the way a policy condition does.
    [Fact] void should_combine_with_and() =>
        ((LogicalConditionSyntax)Requirement.Condition).Operator.ShouldEqual(LogicalOperator.And);
    [Fact] void should_carry_the_state_operand() =>
        ((ComparisonConditionSyntax)((LogicalConditionSyntax)Requirement.Condition).Left).Left
            .ShouldEqual("EngagementScope.isStarted");

    RequirementSyntax Requirement =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single()
            .Validations.OfType<DeclarativeValidateSyntax>().Single().Requirements!.Single();
}
