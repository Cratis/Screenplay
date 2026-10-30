// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    SemanticValue? ResolveDestination(SemanticExpression? expression) => expression is null
        ? request.AllocatedIdentities.GetValueOrDefault(command.Id)
        : Evaluate(expression);

    SemanticExecutionResult? HandlePossibleEvents(string requirement)
    {
        var result = handlers.Handle(command, CreateContext(), cancellationToken);
        var outcome = SemanticHandlerResultValidation.Validate(plan, command, _commandValues, result);
        if (outcome.Unavailable is { } unavailable) return HandlerUnsupported(requirement, unavailable);
        if (outcome.Violation is { } violation) return HandlerUnsupported(requirement, violation.Describe());
        foreach (var item in outcome.Items)
        {
            if (Produce(item.Entry, ((SemanticCompositeValue)item.Payload).Properties, item.Destination) is { } refusal) return refusal;
        }
        return null;
    }
}
