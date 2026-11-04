// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_evaluating_clock_operands;

// 23:30 UTC on 1 March is already 2 March in Europe/Oslo, so the day only matches when the scenario zone reaches the invoked command.
public class with_a_reaction_invoked_command : given.a_clock_command
{
    const string Header =
        """
        module M
          feature F
            slice StateChange S
              event Seen
                id String
                dueDate Date
              event Done
                id String
              reaction R
                when Seen
                  id
                  dueDate
                  invokes C
                    id = id
                    dueDate = dueDate
              command C
                id String identifier
                dueDate Date
        """;

    const string Zoned =
        """
              specification ZonedDay
                given time zone "Europe/Oslo"
                given clock "2025-03-01T23:30:00Z"
                when append Seen
                  id = "a"
                  dueDate = "2025-03-02"
                then Done
                  for "a"
                  id = "a"
              specification UnzonedDay
                given clock "2025-03-01T23:30:00Z"
                when append Seen
                  id = "a"
                  dueDate = "2025-03-02"
                then Done
                  for "a"
                  id = "a"
        """;

    const string ProducesWhen = "        produces when dueDate == today\n          Done\n            for id\n            id = id\n";
    const string CalendarFromContext = "        calendar from $context.timeZone\n        validate\n          require dueDate == today\n        produces Done\n          for id\n          id = id\n";

    const string CalendarFromDeclaredMember = "        calendar from $context.billingZone\n        validate\n          require dueDate == today\n        produces Done\n          for id\n          id = id\n";
    const string DeclaredContext = "context\n  billingZone time zone\n";

    SemanticSpecificationRun _producesWhenInZone = null!;
    SemanticSpecificationRun _producesWhenWithoutZone = null!;
    SemanticSpecificationRun _contextCalendarInZone = null!;
    SemanticSpecificationRun _contextCalendarWithoutZone = null!;
    SemanticSpecificationRun _declaredCalendarInZone = null!;
    SemanticSpecificationRun _declaredCalendarWithoutZone = null!;

    SemanticSpecificationRun Perform(string commandBody, string specification, string context = "")
    {
        Compile(context + Header + "\n" + commandBody + Zoned);
        return new SemanticSpecificationRunner().Run(Plan, Plan.Specifications.Values.Single(value => value.Name == specification).Id);
    }

    void Because()
    {
        _producesWhenInZone = Perform(ProducesWhen, "ZonedDay");
        _producesWhenWithoutZone = Perform(ProducesWhen, "UnzonedDay");
        _contextCalendarInZone = Perform(CalendarFromContext, "ZonedDay");
        _contextCalendarWithoutZone = Perform(CalendarFromContext, "UnzonedDay");
        _declaredCalendarInZone = Perform(CalendarFromDeclaredMember, "ZonedDay", DeclaredContext);
        _declaredCalendarWithoutZone = Perform(CalendarFromDeclaredMember, "UnzonedDay", DeclaredContext);
    }

    [Fact] void should_evaluate_today_in_the_given_zone_for_a_reaction_invoked_produces_when() => Assert.True(_producesWhenInZone.Passed, string.Join('\n', _producesWhenInZone.Failures));
    [Fact] void should_evaluate_today_in_utc_for_a_reaction_invoked_produces_when_without_a_zone() => _producesWhenWithoutZone.Passed.ShouldBeFalse();
    [Fact] void should_supply_the_given_zone_as_the_context_zone_of_a_reaction_invoked_calendar() => Assert.True(_contextCalendarInZone.Passed, string.Join('\n', _contextCalendarInZone.Failures));
    [Fact] void should_not_fall_back_to_utc_for_a_reaction_invoked_context_calendar_without_a_zone() => _contextCalendarWithoutZone.Passed.ShouldBeFalse();
    [Fact] void should_supply_the_given_zone_to_a_declared_member() => Assert.True(_declaredCalendarInZone.Passed, string.Join('\n', _declaredCalendarInZone.Failures));
    [Fact] void should_not_fall_back_to_utc_for_a_declared_member_without_a_zone() => _declaredCalendarWithoutZone.Passed.ShouldBeFalse();
}
