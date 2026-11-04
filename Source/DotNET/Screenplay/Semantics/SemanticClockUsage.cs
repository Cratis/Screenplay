// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

// Finds the application calendar and clock operands in a bound graph, so the staging seam can refuse them on write.
internal static class SemanticClockUsage
{
    internal static bool Uses(SemanticApplication application) =>
        application.TimeZone is not null ||
        !application.ContextMembers.IsDefaultOrEmpty ||
        application.Concepts.Any(concept => concept.Validations.Any(UsesRule)) ||
        application.Modules.SelectMany(module => module.Features).SelectMany(Slices).Any(UsesSlice);

    internal static bool UsesCommand(SemanticCommand command) =>
        command.Calendar is not null ||
        command.Validations.Any(UsesRule) ||
        command.Requirements.Any(requirement => UsesCondition(requirement.Condition)) ||
        command.Produces.Any(production => production.When is { } when && UsesCondition(when));

    internal static bool UsesCondition(SemanticCondition condition) => condition switch
    {
        SemanticLogicalCondition logical => UsesCondition(logical.Left) || UsesCondition(logical.Right),
        SemanticComparison comparison => UsesOperand(comparison.Left) || UsesOperand(comparison.Right),
        _ => false
    };

    // Every graph position is scanned: a clock marker outside the admitted command positions must be refused on write, never dropped.
    static bool UsesRule(SemanticValidationRule rule) => rule.ClockOperand is not null;

    static bool UsesReaction(SemanticReaction reaction) =>
        reaction.Triggers.Any(trigger => (trigger.Where is { } where && UsesCondition(where)) || trigger.Produces.Any(production => production.When is { } when && UsesCondition(when)));

    static bool UsesOperand(SemanticConditionOperand operand) => operand.Clock is not null || operand.DateOf;

    static bool UsesSlice(SemanticSlice slice) =>
        slice.Commands.Any(UsesCommand) || slice.Reactions.Any(UsesReaction) || slice.Specifications.Any(specification => specification.GivenTimeZone is not null);

    static IEnumerable<SemanticSlice> Slices(SemanticFeature feature) => feature.Slices.Concat(feature.Features.SelectMany(Slices));
}
