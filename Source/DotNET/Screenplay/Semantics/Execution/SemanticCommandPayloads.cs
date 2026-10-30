// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

static class SemanticCommandPayloads
{
    internal static ImmutableArray<ImmutableArray<SemanticPropertyValue>> Validate(SemanticExecutionPlan plan, SemanticCommand command, SemanticCommandBodyResult result)
    {
        if (result.Diagnostic is not null) throw new InvalidSemanticContract(result.Diagnostic);
        if (result.Payloads.IsDefault || result.Payloads.Length != command.Produces.Length)
        {
            throw new InvalidSemanticContract("The handler must return one composite payload per produces line.");
        }
        var validator = new SemanticValueValidator(plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), plan.Model.Application.Types.ToDictionary(type => type.Id));
        return [.. command.Produces.Zip(result.Payloads).Select(pair => ValidateEvent(plan, validator, pair.First, pair.Second))];
    }

    static ImmutableArray<SemanticPropertyValue> ValidateEvent(SemanticExecutionPlan plan, SemanticValueValidator validator, SemanticProducedEvent produced, SemanticValue payload)
    {
        if (payload is not SemanticCompositeValue composite || !SemanticValueRules.ValidProperties(composite.Properties))
        {
            throw new InvalidSemanticContract("The handler returned a malformed event payload.");
        }
        var contract = plan.Events[produced.EventContract];
        if (composite.Properties.Any(value => contract.Properties.All(property => property.Id != value.TargetProperty)))
        {
            throw new InvalidSemanticContract("The handler returned an unknown event property.");
        }
        return [.. contract.Properties.Select(property => ValidateProperty(validator, composite, property))];
    }

    static SemanticPropertyValue ValidateProperty(SemanticValueValidator validator, SemanticCompositeValue composite, SemanticProperty property)
    {
        var supplied = composite.Properties.SingleOrDefault(value => value.TargetProperty == property.Id);
        if (supplied is null && !property.Type.IsOptional) throw new InvalidSemanticContract($"Required handler event property '{property.Name}' is missing.");
        var value = supplied?.Value ?? SemanticValue.Null;
        validator.Validate(value, property.Type, $"handler event property '{property.Name}'");
        return new(property.Id, value);
    }
}
