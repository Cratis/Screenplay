// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

public class with_a_declared_context_zone : given.a_clock_command
{
    // 23:30 UTC on March 1 is already March 2 in Oslo, so a due date of March 1 is in the past there and today in UTC.
    static readonly DateTimeOffset Instant = new(2025, 3, 1, 23, 30, 0, TimeSpan.Zero);

    SemanticExecutionResult _supplied = null!;
    SemanticExecutionResult _pastInTheSuppliedZone = null!;
    SemanticExecutionResult _utc = null!;
    SemanticExecutionResult _missing = null!;
    SemanticExecutionResult _onlyTheFixedMember = null!;
    SemanticExecutionResult _empty = null!;
    SemanticExecutionResult _unknown = null!;

    void Establish() => Compile(Source(
        "        calendar from $context.billingZone\n        validate\n          require dueDate >= today\n        produces Done\n          for id\n",
        "context\n  billingZone time zone\n"));

    void Because()
    {
        _supplied = Run("2025-03-02", Instant, new() { ContextZones = Zones("Europe/Oslo") });
        _pastInTheSuppliedZone = Run("2025-03-01", Instant, new() { ContextZones = Zones("Europe/Oslo") });
        _utc = Run("2025-03-01", Instant, new() { ContextZones = Zones("UTC") });
        _missing = Run("2025-03-02", Instant);
        _onlyTheFixedMember = Run("2025-03-02", Instant, new() { ContextZone = "Europe/Oslo" });
        _empty = Run("2025-03-02", Instant, new() { ContextZones = Zones(" ") });
        _unknown = Run("2025-03-02", Instant, new() { ContextZones = Zones("Mars/Olympus") });
    }

    static ImmutableDictionary<string, string> Zones(string zone) => ImmutableDictionary<string, string>.Empty.Add("billingZone", zone);

    [Fact] void should_evaluate_today_in_the_supplied_member_zone() => _supplied.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_reject_a_date_in_the_past_of_the_supplied_zone() => _pastInTheSuppliedZone.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_evaluate_today_in_another_supplied_zone() => _utc.ShouldBeOfExactType<SemanticAccepted>();
    [Fact] void should_fail_when_the_member_is_missing() => _missing.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_not_fall_back_to_the_fixed_context_zone() => _onlyTheFixedMember.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_fail_when_the_member_is_empty() => _empty.ShouldBeOfExactType<SemanticRejected>();
    [Fact] void should_fail_when_the_member_is_not_an_iana_zone() => _unknown.ShouldBeOfExactType<SemanticRejected>();
}
