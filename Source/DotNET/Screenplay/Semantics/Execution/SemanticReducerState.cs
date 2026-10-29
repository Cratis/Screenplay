// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

static class SemanticReducerState
{
    internal static ImmutableArray<SemanticPropertyValue> Validate(SemanticExecutionPlan plan, SemanticReadModel model, SemanticCompositeValue state, SemanticValue key)
    {
        if (!SemanticValueRules.ValidProperties(state.Properties)) throw new InvalidSemanticContract("The reducer state has malformed properties.");
        if (state.Properties.Any(value => model.Properties.All(property => property.Id != value.TargetProperty))) throw new InvalidSemanticContract("The reducer state has an unknown property.");
        var validator = new SemanticValueValidator(plan.Model.Application.Concepts.ToDictionary(concept => concept.Id), plan.Model.Application.Types.ToDictionary(type => type.Id));
        var result = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
        foreach (var property in model.Properties)
        {
            result.Add(ValidateProperty(property, state, validator, key));
        }
        return result.ToImmutable();
    }

    static SemanticPropertyValue ValidateProperty(SemanticProperty property, SemanticCompositeValue state, SemanticValueValidator validator, SemanticValue key)
    {
        var supplied = state.Properties.SingleOrDefault(value => value.TargetProperty == property.Id);
        if (supplied is null && !property.Type.IsOptional) throw new InvalidSemanticContract($"Required reducer state property '{property.Name}' is missing.");
        var value = supplied?.Value ?? SemanticValue.Null;
        validator.Validate(value, property.Type, $"reducer state property '{property.Name}'");
        if (property.IsIdentifier && !SemanticValueRules.AreEqual(value, key)) throw new InvalidSemanticContract($"Reducer state identifier '{property.Name}' disagrees with its event source key.");
        return new(property.Id, value);
    }
}
