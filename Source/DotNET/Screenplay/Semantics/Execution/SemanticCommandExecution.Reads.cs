// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed partial class SemanticCommandExecution
{
    SemanticRejected? ReadRequired()
    {
        foreach (var read in command.Reads)
        {
            var key = _commandValues[read.Key];
            var instance = world.ReadModels.FirstOrDefault(instance => instance.ReadModel == read.ReadModel && SemanticValueRules.AreEqual(instance.Key, key));
            if (instance is null)
            {
                var message = $"{plan.ReadModels[read.ReadModel].Name} '{((SemanticTextValue)key).Value}' does not exist.";
                return SemanticEvaluator.RejectWithMessage(world, SemanticRejectionCategory.Validation, null, message) with
                {
                    ValidationFailures = [new(message, SemanticValidationSeverity.Error)]
                };
            }

            _reads.Add(read.Name, instance.Values.ToDictionary(value => value.TargetProperty, value => value.Value));
        }

        return null;
    }
}
