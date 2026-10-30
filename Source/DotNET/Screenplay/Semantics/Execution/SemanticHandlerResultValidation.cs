// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal static class SemanticHandlerResultValidation
{
    internal const int DefaultMaximumItems = 1_000;

    internal static SemanticHandlerResultOutcome Validate(
        SemanticExecutionPlan plan,
        SemanticCommand command,
        IReadOnlyDictionary<SemanticId, SemanticValue> commandValues,
        SemanticCommandBodyResult result,
        int maximumItems = DefaultMaximumItems)
    {
        // Parent-approved S4 ordering: diagnostic first, then legacy payloads even when Items is default,
        // then absent v2 items. This distinguishes a wrong result contract from an unavailable result.
        if (result.Diagnostic is not null) return new([], result.Diagnostic, null);
        if (!result.Payloads.IsDefaultOrEmpty) return Refuse(null, "result v2 handlers return items, not payloads");
        if (result.Items.IsDefault) return new([], "handler returned no v2 result", null);
        if (result.Items.Length > maximumItems) return Refuse(maximumItems, $"handler returned more than {maximumItems} result items");

        var validator = new SemanticValueValidator(plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), plan.Model.Application.Types.ToDictionary(type => type.Id));
        var items = ImmutableArray.CreateBuilder<SemanticHandlerResultItem>();
        for (var index = 0; index < result.Items.Length; index++)
        {
            var (item, reason) = ValidateItem(plan, validator, command, commandValues, result.Items[index]);
            if (reason is not null) return Refuse(index, reason);
            items.Add(item!);
        }
        return new(items.ToImmutable(), null, null);
    }

    static (SemanticHandlerResultItem? Item, string? Reason) ValidateItem(SemanticExecutionPlan plan, SemanticValueValidator validator, SemanticCommand command, IReadOnlyDictionary<SemanticId, SemanticValue> commandValues, SemanticCommandBodyItem item)
    {
        if (item is null) return (null, "handler returned a null item");
        var entry = command.Handler!.PossibleEvents.SingleOrDefault(entry => entry.EventContract == item.EventContract);
        if (entry is null) return (null, $"event {item.EventContract} is not a possible event");
        if (DestinationViolation(entry, item, commandValues) is { } reason) return (null, reason);
        try
        {
            var values = SemanticCommandPayloads.ValidateEvent(validator, plan.Events[entry.EventContract], item.Payload, "handler");
            var destination = ResolveDestination(entry, item, commandValues);
            return (new(entry, SemanticValue.Composite(values), destination), null);
        }
        catch (InvalidSemanticContract exception)
        {
            return (null, exception.Message);
        }
    }

    static SemanticValue? ResolveDestination(SemanticProducedEvent entry, SemanticCommandBodyItem item, IReadOnlyDictionary<SemanticId, SemanticValue> commandValues) => entry.Destination switch
    {
        SemanticResolvedExpression property => commandValues.GetValueOrDefault(property.Target, SemanticValue.Null),
        SemanticOneOfExpression => item.Destination,
        _ => null
    };

    static string? DestinationViolation(SemanticProducedEvent entry, SemanticCommandBodyItem item, IReadOnlyDictionary<SemanticId, SemanticValue> commandValues)
    {
        if (entry.Destination is not SemanticOneOfExpression oneOf)
        {
            return item.Destination is null ? null : "only one-of entries accept a handler destination";
        }
        return item.Destination is not null && commandValues.GetValueOrDefault(oneOf.CollectionProperty) is SemanticArrayValue collection &&
            collection.Values.Any(value => SemanticValueRules.AreEqual(value, item.Destination))
                ? null
                : "destination is not a member of the command collection";
    }

    static SemanticHandlerResultOutcome Refuse(int? index, string reason) => new([], null, new(index, reason));
}
