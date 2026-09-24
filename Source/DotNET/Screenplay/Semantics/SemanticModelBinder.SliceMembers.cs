// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        ImmutableArray<SemanticReducer> BindReducers(SemanticAddress owner, SliceSyntax slice)
        {
            var reducers = ImmutableArray.CreateBuilder<SemanticReducer>();
            foreach (var reducer in slice.Reducers ?? [])
            {
                var rules = reducer.Rules.ToArray();
                var seenEvents = new HashSet<SemanticId>();
                var resolvedRules = new List<(ReducerRuleSyntax Rule, SemanticId EventId)>();
                foreach (var rule in rules)
                {
                    if (!_events.TryGetValue(ShortName(rule.Event), out var @event))
                    {
                        Error(DiagnosticCodes.InvalidSemanticBinding, $"Reducer '{reducer.Name}' event '{rule.Event}' is unresolved.", rule.Location);
                        continue;
                    }

                    if (!seenEvents.Add(@event.Contract.Id))
                    {
                        Error(DiagnosticCodes.DuplicateReducerEvent, $"Reducer '{reducer.Name}' observes event '{rule.Event}' more than once.", rule.Location);
                        continue;
                    }

                    resolvedRules.Add((rule, @event.Contract.Id));
                    RequireImplementation(SemanticImplementationRole.ReducerTransition, owner, rule.File, rule.Code, $"{reducer.Name}/on {@event.Contract.Id}");
                }

                if (rules.All(rule => rule.File is null && rule.Code is null))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, UnsupportedReducerMessage(reducer), reducer.Location);
                    continue;
                }

                if (rules.Any(rule => rule.File is null && rule.Code is null))
                {
                    Error(DiagnosticCodes.IncompleteReducerTransitions, $"Reducer '{reducer.Name}' mixes rules with and without transition bodies. Give each 'on <Event>' a body.", reducer.Location);
                    continue;
                }

                if (!_readModels.TryGetValue(ShortName(reducer.ReadModel), out var readModel))
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Reducer '{reducer.Name}' read model is unresolved.", reducer.Location);
                    continue;
                }

                var transitions = ImmutableArray.CreateBuilder<SemanticReducerTransition>();
                foreach (var (rule, eventId) in resolvedRules)
                {
                    var member = $"{reducer.Name}/on {eventId}";
                    var requirement = _implementationRequirements.SingleOrDefault(value => value.Role == SemanticImplementationRole.ReducerTransition && Equals(value.Owner, owner) && value.Member == member);
                    if (requirement is not null)
                    {
                        transitions.Add(new(eventId, requirement.RequirementId));
                    }
                }

                reducers.Add(new(reducer.Name, readModel.Model.Id, transitions.ToImmutable()));
                UsesV3 = true;
            }

            return reducers.ToImmutable();
        }

        void ReportUnsupportedSliceMembers(SemanticAddress owner, SliceSyntax slice)
        {
            foreach (var reaction in slice.Reactions)
            {
                foreach (var trigger in reaction.Triggers)
                {
                    var source = trigger.Source switch
                    {
                        NamedTriggerSourceSyntax named => $"when {named.Name}",
                        IntervalTriggerSourceSyntax interval => $"every {interval.Amount.ToString(CultureInfo.InvariantCulture)} {interval.Unit}",
                        ScheduleTriggerSourceSyntax schedule => $"at {schedule.Time.ToString("HH':'mm", CultureInfo.InvariantCulture)}/{schedule.DayOfWeek}/{schedule.DayOfMonth?.ToString(CultureInfo.InvariantCulture)}",
                        _ => throw new InvalidSemanticContract("An unknown reaction trigger source cannot identify an implementation.")
                    };
                    RequireImplementation(SemanticImplementationRole.ReactionEffect, owner, trigger.File, trigger.Code, $"{reaction.Name}/{source}");
                }

                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Reaction '{reaction.Name}' requires portable occurrence and effect semantics.", reaction.Location);
            }

            foreach (var capture in slice.Captures)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Capture '{capture.Name}' requires a portable compiled CDL plan.", capture.Location);
            }

            foreach (var screen in slice.Screens)
            {
                Information(DiagnosticCodes.DeferredSemanticSyntax, $"Screen '{screen.Name}' is explicitly deferred from the backend ESM v1 profile.", screen.Location);
            }
        }

        string UnsupportedReducerMessage(ReducerSyntax reducer) =>
            $"Reducer '{reducer.Name}' has no transition body. A reducer that only lists events is a projection - declare 'projection <Name> => {reducer.ReadModel}' - or give each 'on <Event>' an inline code block or 'file <path>'.";
    }
}
