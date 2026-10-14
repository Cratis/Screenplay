// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Classifies protected command refusals under decision 0030 and Arc 22.88.0's first-match contract.
/// </summary>
internal sealed record SemanticInvocationRefusalEvaluation(string Reason, ImmutableArray<SemanticConstraintFailure> Constraints, string Message)
{
    internal static SemanticInvocationRefusalEvaluation? Classify(SemanticExecutionResult result)
    {
        if (result is not SemanticRejected rejected || rejected.Category is SemanticRejectionCategory.Contract or SemanticRejectionCategory.Concurrency) return null;
        if (rejected.Category == SemanticRejectionCategory.Unauthorized) return new("authorization", [], rejected.Details);
        var constraints = rejected.ConstraintFailures;
        if (constraints.IsDefaultOrEmpty && rejected.Category == SemanticRejectionCategory.Constraint && rejected.Code is { } name)
        {
            constraints = [new(name, rejected.Details)];
        }
        if (!constraints.IsDefaultOrEmpty) return new("constraint", constraints, string.Join('\n', constraints.Select(failure => failure.Message)));
        var errors = rejected.ValidationFailures.Where(failure => failure.Severity == SemanticValidationSeverity.Error).ToArray();
        if (errors.Length > 0) return new("validation", [], string.Join('\n', errors.Select(failure => failure.Message)));
        if (rejected.Category == SemanticRejectionCategory.Validation && rejected.ValidationFailures.IsDefaultOrEmpty) return new("validation", [], rejected.Details);

        return null;
    }

    internal bool Matches(SemanticInvocationRefusal branch) => branch.Selector switch
    {
        "any" => Reason == "validation" || Reason == "constraint",
        "constraint" => Reason == "constraint" && (branch.Constraint is null || Constraints.Any(failure => failure.Name == branch.Constraint)),
        _ => branch.Selector == Reason
    };

    internal SemanticValue ValueOf(string member) => SemanticValue.Text(member switch
    {
        "reason" => Reason,
        "constraint" when !Constraints.IsDefaultOrEmpty => Constraints[0].Name,
        "message" => Message,
        _ => throw new InvalidSemanticContract("A refusal expression has an unavailable member.")
    });
}
