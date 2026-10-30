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
        return [.. command.Produces.Zip(result.Payloads).Select(pair => ValidateEvent(validator, plan.Events[pair.First.EventContract], pair.Second, "handler"))];
    }

    internal static ImmutableArray<SemanticPropertyValue> ValidateEvent(SemanticValueValidator validator, SemanticEventContract contract, SemanticValue payload, string label)
    {
        if (payload is not SemanticCompositeValue composite || !SemanticValueRules.ValidProperties(composite.Properties))
        {
            throw new InvalidSemanticContract($"The {label} returned a malformed event payload.");
        }
        if (composite.Properties.Any(value => contract.Properties.All(property => property.Id != value.TargetProperty)))
        {
            throw new InvalidSemanticContract($"The {label} returned an unknown event property.");
        }
        return [.. contract.Properties.Select(property => ValidateProperty(validator, composite, property, $"{label} event"))];
    }

    internal static ImmutableArray<SemanticPropertyValue> ValidateCommand(SemanticValueValidator validator, SemanticCommand command, SemanticValue payload, string label)
    {
        if (payload is not SemanticCompositeValue composite || !SemanticValueRules.ValidProperties(composite.Properties))
        {
            throw new InvalidSemanticContract($"The {label} returned a malformed command payload.");
        }
        if (composite.Properties.Any(value => command.Properties.All(property => property.Id != value.TargetProperty || property.IsGenerated)))
        {
            throw new InvalidSemanticContract($"The {label} returned an unknown or generated command property.");
        }
        return [.. command.Properties.Where(property => !property.IsGenerated).Select(property => ValidateProperty(validator, composite, property, $"{label} command"))];
    }

    static SemanticPropertyValue ValidateProperty(SemanticValueValidator validator, SemanticCompositeValue composite, SemanticProperty property, string label)
    {
        var supplied = composite.Properties.SingleOrDefault(value => value.TargetProperty == property.Id);
        if (supplied is null && !property.Type.IsOptional) throw new InvalidSemanticContract($"Required {label} property '{property.Name}' is missing.");
        var value = supplied?.Value ?? SemanticValue.Null;
        validator.Validate(value, property.Type, $"{label} property '{property.Name}'");
        return new(property.Id, value);
    }
}
