// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesRefusalBranches(SemanticApplication application)
    {
        var invocations = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices)
            .SelectMany(slice => slice.Reactions).SelectMany(reaction => reaction.Triggers)
            .SelectMany(trigger => trigger.Invokes).ToArray();
        foreach (var invocation in invocations)
        {
            if (invocation is null || invocation.OnRefused.IsDefault || invocation.OnRefused.Any(branch => branch is null || branch.Produces.IsDefault || branch.Produces.Any(produced => produced is null)))
            {
                throw new InvalidSemanticContract("Refusal branch and production collections must be initialized and contain no null elements.");
            }
        }

        return invocations.Any(invocation => !invocation.OnRefused.IsEmpty);
    }

    static void ValidateRefusalVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesRefusalBranches(application),
            version,
            SemanticVersion.V19,
            "An ESM v19 model must contain reaction refusal branches or refusal values.",
            "Reaction refusal branches and refusal values require ESM v19.");

    private sealed partial class ValidationContext
    {
        string? _refusalSelector;

        void ValidateInvocationRefusals(SemanticReaction reaction, SemanticReactionTrigger trigger, SemanticInvocation invocation, SemanticCommand command, SemanticExpressionRootKind root, Dictionary<SemanticId, SemanticProperty> values)
        {
            RequireObjects(invocation.OnRefused, nameof(invocation.OnRefused), "refusal branch");
            foreach (var branch in invocation.OnRefused)
            {
                if (branch.Selector is not ("any" or "validation" or "constraint" or "authorization") ||
                    (branch.Constraint is not null && branch.Selector != "constraint"))
                {
                    throw new InvalidSemanticContract("A refusal branch has an invalid selector or constraint shape.");
                }

                if (branch.Constraint is { } name)
                {
                    var constraint = _eventRoutesApplication.Modules.SelectMany(module => module.Features).SelectMany(SemanticModelValidator.AllSlices)
                        .SelectMany(slice => slice.Constraints).SingleOrDefault(constraint => constraint.Name == name);
                    if (constraint?.Targets.Any(target => command.Produces.Any(produced => produced.EventContract == target.EventContract)) != true)
                    {
                        throw new InvalidSemanticContract("A named refusal constraint must resolve and target an event produced by the invoked command.");
                    }
                }

                RequireObjects(branch.Produces, nameof(branch.Produces), "refusal produced event");
                _refusalSelector = branch.Selector;
                try
                {
                    foreach (var produced in branch.Produces)
                    {
                        if (produced.Route is not null) throw new InvalidSemanticContract("Reaction refusal productions cannot declare routes.");
                        ValidateReactionProduces(reaction, trigger, produced, root, values);
                    }
                }
                finally
                {
                    _refusalSelector = null;
                }
            }
        }

        SemanticTypeReference ResolveRefusalExpression(SemanticRefusalExpression expression)
        {
            if (!_semanticVersion.IsAtLeast(SemanticVersion.V19) || _refusalSelector is null ||
                expression.Member is not ("reason" or "constraint" or "message") ||
                (expression.Member == "constraint" && _refusalSelector != "constraint") ||
                expression.Type != SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text))
            {
                throw new InvalidSemanticContract("A refusal expression requires ESM v19, a branch mapping scope and the String type; constraint requires a constraint branch.");
            }

            return expression.Type;
        }
    }
}
