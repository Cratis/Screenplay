// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    SemanticExecutionResult? Handle(string requirement)
    {
        if (!handlers.HasProvider)
        {
            return new SemanticUnsupported(world, SemanticExecutionCapability.ImplementationBody, $"Command '{command.Name}' handler body requires a target (requirement {requirement}); the reference executor does not run implementation bodies.");
        }

        // Result v1 owns only payloads. It cannot decide cardinality or make a conditional production portable.
        if (command.Produces.IsDefaultOrEmpty || command.Produces.Any(produced => produced.When is not null || produced.Condition is not null))
        {
            return HandlerUnsupported(requirement, "Only unconditional produces contracts are supported.");
        }
        var context = new SemanticCommandContext(
            new([.. _commandValues.Select(value => new SemanticPropertyValue(value.Key, value.Value))]),
            request.Tenant,
            request.Caller is { Roles.IsDefault: false, Claims.IsDefault: false } caller ? SemanticBodyIdentity.From(caller) : null,
            request.Occurrence is { IsTimeOnly: false } occurrence ? new(occurrence.Subject, occurrence.Name, occurrence.UserName) : null,
            null,
            request.Occurrence?.Occurred);
        ImmutableArray<ImmutableArray<SemanticPropertyValue>> payloads;
        try
        {
            payloads = SemanticCommandPayloads.Validate(plan, command, handlers.Handle(command, context, cancellationToken));
        }
        catch (InvalidSemanticContract exception)
        {
            return HandlerUnsupported(requirement, exception.Message);
        }
        foreach (var pair in command.Produces.Zip(payloads))
        {
            if (Produce(pair.First, pair.Second) is { } refusal) return refusal;
        }
        return null;
    }

    SemanticUnsupported HandlerUnsupported(string requirement, string diagnostic) => new(
        world,
        SemanticExecutionCapability.ImplementationBody,
        $"Command '{command.Name}' handler (requirement {requirement}): {SemanticRuleBodyExecution.Sanitize(diagnostic)}");
}
