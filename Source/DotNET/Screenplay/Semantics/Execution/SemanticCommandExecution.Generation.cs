// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    // Validate every supplied entry before checking completeness. An invalid later fixture must not be
    // hidden by an earlier missing property, and fixtures never feed the legacy allocated-destination channel.
    SemanticExecutionResult? Generate()
    {
        var generated = command.Properties.Where(property => property.IsGenerated).ToDictionary(property => property.Id);
        if (!request.GeneratedValues.IsDefaultOrEmpty && AcceptFixtures(generated) is { } rejection)
        {
            return rejection;
        }

        if (generated.Values.FirstOrDefault(property => !_commandValues.ContainsKey(property.Id)) is { } missing)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.IdentityAllocation, $"Command '{command.Name}' requires a deterministic generated value for '{missing.Name}'.");
        }

        return null;
    }

    SemanticRejected? AcceptFixtures(Dictionary<SemanticId, SemanticProperty> generated)
    {
        var validator = new SemanticValueValidator(
            plan.Model.Application.Concepts.ToDictionary(concept => concept.Id),
            plan.Model.Application.Types.ToDictionary(type => type.Id));
        var seen = new HashSet<SemanticId>();
        foreach (var fixture in request.GeneratedValues)
        {
            if (fixture is null || !generated.TryGetValue(fixture.TargetProperty, out var property) || !seen.Add(fixture.TargetProperty))
            {
                return Contract($"Command '{command.Name}' generated values do not match its generated property shape.");
            }

            if (ValidateFixture(validator, fixture, property) is { } rejection)
            {
                return rejection;
            }

            _commandValues.Add(fixture.TargetProperty, fixture.Value);
        }

        return null;
    }

    SemanticRejected? ValidateFixture(SemanticValueValidator validator, SemanticPropertyValue fixture, SemanticProperty property)
    {
        try
        {
            validator.Validate(fixture.Value, property.Type, $"generated command property '{property.Name}'");
            var concept = plan.Model.Application.Concepts.Single(concept => concept.Id == property.Type.Target);
            if ((SemanticModelValidator.UsesConceptGeneration(plan.Model.Application) || !concept.Validations.IsEmpty) &&
                !SemanticGeneratedFixture.Matches(concept, fixture.Value, request.Occurrence?.Occurred))
            {
                return Contract($"Generated fixture for '{property.Name}' does not match its generation strategy shape.");
            }

            if (concept.Validations.Any(rule => !SemanticValidationRules.Satisfies(rule, fixture.Value)))
            {
                return Contract($"Generated fixture '{property.Name}' fails concept '{concept.Name}' rules");
            }

            return null;
        }
        catch (InvalidSemanticContract exception)
        {
            return Contract(exception.Message);
        }
    }
}
