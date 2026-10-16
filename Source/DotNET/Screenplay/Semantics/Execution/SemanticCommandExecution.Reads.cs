// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    SemanticExecutionResult? ReadRequired()
    {
        foreach (var read in command.Reads)
        {
            if (plan.IsUnadmittedReadModel(read.ReadModel))
            {
                return new SemanticUnsupported(world, SemanticExecutionCapability.Projection, $"Read model '{plan.ReadModels[read.ReadModel].Name}' is not admitted by scoped execution-plan compilation.");
            }

            var key = _commandValues[read.Key];
            var instance = world.ReadModels.FirstOrDefault(instance => instance.ReadModel == read.ReadModel && SemanticValueRules.AreEqual(instance.Key, key));
            if (instance is null) return MissingRead(read, key);

            _reads.Add(read.Name, instance.Values.ToDictionary(value => value.TargetProperty, value => value.Value));
        }

        return null;
    }

    SemanticRejected MissingRead(SemanticDecisionRead read, SemanticValue key)
    {
        var message = $"{plan.ReadModels[read.ReadModel].Name} '{((SemanticTextValue)key).Value}' does not exist.";
        return SemanticEvaluator.RejectWithMessage(world, SemanticRejectionCategory.Validation, null, message) with
        {
            ValidationFailures = [new(message, SemanticValidationSeverity.Error)]
        };
    }
}
