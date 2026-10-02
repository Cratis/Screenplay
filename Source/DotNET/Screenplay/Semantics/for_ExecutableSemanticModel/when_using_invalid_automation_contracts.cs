// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_automation_contracts : Specification
{
    ExecutableSemanticModel _model;

    void Establish() => _model = canonical_serialization_golden_vectors.CreateSemanticModelV6();

    [Fact] void should_admit_the_golden_v6_model() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    [Fact] void should_refuse_v6_constructs_in_a_v5_model() => Refuses(_model.Application, SemanticVersion.V5);
    [Fact] void should_refuse_a_reaction_in_a_state_change_slice() => Refuses(Slice("Automations", slice => slice with { Kind = SemanticSliceKind.StateChange }));
    [Fact] void should_refuse_a_capture_outside_a_translate_slice() => Refuses(Slice("LegacySync", slice => slice with { Kind = SemanticSliceKind.Automation }));
    [Fact] void should_refuse_an_empty_interval() => Refuses(Trigger(SemanticReactionTriggerKind.Interval, trigger => trigger with { Every = 0 }));
    [Fact] void should_refuse_a_schedule_on_both_a_weekday_and_a_day_of_the_month() => Refuses(Trigger(SemanticReactionTriggerKind.Interval, _ => new(SemanticReactionTriggerKind.Schedule) { At = 0, OnDayOfWeek = 1, OnDayOfMonth = 1 }));
    [Fact] void should_refuse_a_time_of_day_past_midnight() => Refuses(Trigger(SemanticReactionTriggerKind.Interval, _ => new(SemanticReactionTriggerKind.Schedule) { At = 86_400 }));
    [Fact] void should_refuse_a_clock_reaction_without_an_event_source() =>
        Refuses(Trigger(SemanticReactionTriggerKind.Interval, trigger => trigger with { Produces = [trigger.Produces.Single() with { Destination = null, DestinationType = null }] }));
    [Fact] void should_refuse_a_capture_condition_outside_the_template_grammar() =>
        Refuses(Slice("LegacySync", slice => slice with
        {
            Captures = [slice.Captures.Single() with { Appends = [.. slice.Captures.Single().Appends.Select(append => append with { When = new(SemanticCaptureConditionKind.Expression, []) { Expression = "`status ~ 1`" } })] }]
        }));
    [Fact] void should_refuse_a_specification_with_two_actions() =>
        Refuses(Specifications("Automations", specification => specification with { WhenClock = "2026-10-05T10:00:00.0000000Z", GivenClock = "2026-10-05T09:00:00.0000000Z" }));
    [Fact] void should_refuse_a_clock_that_moves_back() =>
        Refuses(Specifications("Automations", specification => specification.WhenClock is null ? specification : specification with { WhenClock = "2026-10-05T06:00:00.0000000Z" }));
    [Fact] void should_refuse_a_clock_that_is_not_round_trip_utc() =>
        Refuses(Specifications("Automations", specification => specification.GivenClock is null ? specification : specification with { GivenClock = "2026-10-05T09:00:00Z" }));
    [Fact] void should_refuse_a_capture_record_without_its_key() =>
        Refuses(Specifications("LegacySync", specification => specification with { WhenCapture = specification.WhenCapture! with { Record = new([.. specification.WhenCapture.Record.Fields.Where(field => field.Name != "id")]) } }));

    void Refuses(SemanticApplication application, SemanticVersion? version = null) =>
        Catch.Exception(() => ExecutableSemanticModel.Create(version is null ? LanguageVersion.V6 : LanguageVersion.V5, version ?? SemanticVersion.V6, application))
            .ShouldBeOfExactType<InvalidSemanticContract>();

    SemanticApplication Slice(string name, Func<SemanticSlice, SemanticSlice> change)
    {
        var module = _model.Application.Modules.Single();
        return _model.Application with
        {
            Modules = [module with { Features = [.. module.Features.Select(feature => feature with { Slices = [.. feature.Slices.Select(slice => slice.Name == name ? change(slice) : slice)] })] }]
        };
    }

    SemanticApplication Trigger(SemanticReactionTriggerKind kind, Func<SemanticReactionTrigger, SemanticReactionTrigger> change) =>
        Slice("Automations", slice => slice with
        {
            Reactions = [.. slice.Reactions.Select(reaction => reaction with { Triggers = [.. reaction.Triggers.Select(trigger => trigger.Kind == kind ? change(trigger) : trigger)] })]
        });

    SemanticApplication Specifications(string slice, Func<SemanticSpecification, SemanticSpecification> change) =>
        Slice(slice, value => value with { Specifications = [.. value.Specifications.Select(change)] });
}
#endif
