// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

sealed class SemanticDecisionExecution(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request, CancellationToken cancellationToken)
{
    internal SemanticDecision Decide(SemanticRuleBodyExecution? rules, SemanticReducerBodyExecution reducers, SemanticPolicyBodyExecution policies, Func<SemanticBodyExecutionRun>? commitRun)
    {
        if (request.IsReadOnly)
        {
            return SemanticDecision.Refused(new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "A read-only request cannot be decided; execute it as a query."));
        }

        if (!plan.Commands.TryGetValue(request.Command, out var command))
        {
            return SemanticDecision.Refused(new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{request.Command}' is not in the execution plan."));
        }

        if (plan.UnadmittedArtifacts.Contains(command.Id))
        {
            return SemanticDecision.Refused(new SemanticUnsupported(world, SemanticExecutionCapability.Command, $"Command '{command.Name}' was not admitted by scoped execution-plan compilation."));
        }

        if (request.Queries.IsDefault)
        {
            return SemanticDecision.Refused(new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "Execution request query collection cannot be default."));
        }

        return new SemanticCommandExecution(plan, world, command, request, rules, reducers, policies, commitRun, cancellationToken).Decide();
    }
}
