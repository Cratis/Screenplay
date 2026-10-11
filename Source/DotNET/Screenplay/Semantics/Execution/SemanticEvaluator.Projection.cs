// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

public sealed partial class SemanticEvaluator
{
    internal static bool TryProject(
        SemanticExecutionPlan plan,
        ImmutableArray<SemanticFact> history,
        ImmutableArray<SemanticReadModelInstance> current,
        ImmutableArray<SemanticFact> facts,
        out ImmutableArray<SemanticReadModelInstance> readModels,
        out string? failure)
    {
        var instances = current.ToList();
        var concepts = plan.Model.Application.Concepts.ToDictionary(_ => _.Id);
        var types = plan.Model.Application.Types.ToDictionary(_ => _.Id);
        var validator = new SemanticValueValidator(concepts, types);
        var observed = history.ToList();
        var projection = new SemanticFactProjection(plan, validator, instances, observed);

        foreach (var fact in facts)
        {
            if (projection.Apply(fact) is { } projectionFailure)
            {
                failure = projectionFailure;
                readModels = current;
                return false;
            }

            observed.Add(fact);
        }

        failure = null;
        readModels = [.. instances];
        return true;
    }
}
