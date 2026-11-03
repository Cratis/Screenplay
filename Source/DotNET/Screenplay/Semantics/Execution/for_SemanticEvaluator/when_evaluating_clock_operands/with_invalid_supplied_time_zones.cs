// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_invalid_supplied_time_zones : given.a_clock_command
{
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);
    const string Requirements = "        validate\n          require dueDate == today\n        produces Done\n          for id\n";

    [Theory]
    [InlineData("   ", "The configured application time zone is empty; supply an IANA time zone id or none.")]
    [InlineData("Mars/Olympus", "The calendar time zone 'Mars/Olympus' is not a known IANA time zone.")]
    [InlineData("W. Europe Standard Time", "The calendar time zone 'W. Europe Standard Time' is not a known IANA time zone.")]
    public void should_report_a_contract_rejection_for_invalid_configuration(string zone, string message)
    {
        Compile(Source(Requirements, "time zone configurable default \"Europe/Oslo\"\n"));
        var result = (SemanticRejected)Run("2025-03-02", Instant, new() { ConfiguredZone = zone });
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.Details.ShouldEqual(message);
    }

    [Theory]
    [InlineData("   ", "Command 'C' reads its calendar from $context.timeZone, and the execution request supplies none.")]
    [InlineData("Mars/Olympus", "The calendar time zone 'Mars/Olympus' is not a known IANA time zone.")]
    [InlineData("W. Europe Standard Time", "The calendar time zone 'W. Europe Standard Time' is not a known IANA time zone.")]
    public void should_report_a_contract_rejection_for_an_invalid_context_zone(string zone, string message)
    {
        Compile(Source("        calendar from $context.timeZone\n" + Requirements, "time zone \"Europe/Oslo\"\n"));
        var result = (SemanticRejected)Run("2025-03-02", Instant, new() { ContextZone = zone, ScenarioZone = "Europe/Oslo" });
        result.Category.ShouldEqual(SemanticRejectionCategory.Contract);
        result.Details.ShouldEqual(message);
    }
}
