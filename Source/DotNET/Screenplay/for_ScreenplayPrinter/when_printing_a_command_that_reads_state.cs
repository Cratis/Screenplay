// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_command_that_reads_state : given.a_printer
{
    const string Source =
        """
        module Timesheets
          feature HourRegistration
            slice StateChange StartMonth
              command StartMonth
                engagementId Uuid
                reads EngagementScope by engagementId
                reads WorkingCalendar

                produces TimesheetStarted
                  engagementId = engagementId
                  consultantId = EngagementScope.consultantId

              event TimesheetStarted
                engagementId Uuid
                consultantId Uuid

            slice StateView Scopes
              projection Scopes => EngagementScope
                from TimesheetStarted key engagementId
                  consultantId = consultantId

              projection Calendars => WorkingCalendar
                from TimesheetStarted key engagementId
                  consultantId = consultantId
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_key_it_reads_by() => _roundtrip.Printed.ShouldContain("reads EngagementScope by engagementId");
    [Fact] void should_print_a_read_model_that_has_no_key() => _roundtrip.Printed.ShouldContain("reads WorkingCalendar");
    [Fact] void should_keep_both_declarations() => Command.Reads!.Count().ShouldEqual(2);
    [Fact] void should_keep_the_mapping_from_state() =>
        ((PathExpressionSyntax)Command.Produces.Single().Mappings.Single(mapping => mapping.Property == "consultantId").Source)
            .Path.ShouldEqual("EngagementScope.consultantId");

    CommandSyntax Command =>
        _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.First().Commands.Single();
}
