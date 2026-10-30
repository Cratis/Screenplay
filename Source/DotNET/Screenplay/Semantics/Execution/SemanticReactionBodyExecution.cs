// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

internal sealed class SemanticReactionBodyExecution(ISemanticBodyExecutionProvider? provider, SemanticRuleBodyExecution? bodies)
{
    internal static readonly SemanticReactionBodyExecution None = new(null, null);
    internal bool HasProvider => provider is not null;

    internal static SemanticReactionBodyExecution For(ISemanticEvaluator evaluator) => evaluator switch
    {
        SemanticEvaluator semantic => semantic.ReactionBodies,
        SemanticCancellationEvaluator scoped => For(scoped.Evaluator),
        _ => None
    };

    internal SemanticReactionBodyResult React(SemanticExecutionPlan plan, SemanticReaction reaction, SemanticReactionContext context, CancellationToken cancellationToken)
    {
        var body = bodies?.FindBody(reaction.Triggers[0].RequirementId, SemanticImplementationRole.ReactionEffect);
        if (!IsVerified(body, plan, reaction, context.EventType))
        {
            return SemanticReactionBodyResult.Unsupported("No resolved, content-verified result-v1 reaction body is available.");
        }
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            var result = Task.Run(async () => await provider!.ReactAsync(body!, context, cancellationToken).ConfigureAwait(false)).GetAwaiter().GetResult();
            cancellationToken.ThrowIfCancellationRequested();
            return result ?? SemanticReactionBodyResult.Unsupported("The provider returned no reaction result.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            return SemanticReactionBodyResult.Unsupported($"Provider threw {exception.GetType().Name}.");
        }
    }

    static bool IsVerified(SemanticImplementationBody? body, SemanticExecutionPlan plan, SemanticReaction reaction, SemanticId eventType) =>
        body?.Requirement is { } requirement && IsResolvedV1(requirement) && HasIdentity(requirement, reaction, plan.Events[eventType].Name) &&
        OwnsReaction(plan, reaction, requirement) && SemanticRuleBodyExecution.MatchesHash(body);

    static bool IsResolvedV1(SemanticImplementationRequirement requirement) =>
        !requirement.IsAmbiguous && !string.IsNullOrWhiteSpace(requirement.Language) &&
        requirement.AttachmentResolution == SemanticAttachmentResolution.Resolved && requirement.ContextVersion == 1 && requirement.ResultVersion == 1;

    static bool HasIdentity(SemanticImplementationRequirement requirement, SemanticReaction reaction, string eventName)
    {
        var member = $"{reaction.Name}/when {eventName}";
        return requirement.Member == member && requirement.RequirementId == ImplementationRequirementIdentity.Create(requirement.Source.SemanticId, SemanticImplementationRole.ReactionEffect, member);
    }

    // Reactions attach to their nearest addressable parent: the slice, not a reaction address.
    static bool OwnsReaction(SemanticExecutionPlan plan, SemanticReaction reaction, SemanticImplementationRequirement requirement) =>
        requirement.Owner.Kind == SemanticKind.Slice && plan.Model.Application.Modules.Any(module =>
            OwnsReaction((module.Name, []), module.Features, reaction, requirement));

    static bool OwnsReaction((string Module, ImmutableArray<string> Path) address, ImmutableArray<SemanticFeature> features, SemanticReaction reaction, SemanticImplementationRequirement requirement) =>
        features.Any(feature => feature.Slices.Any(slice => slice.Id == requirement.Source.SemanticId && slice.Reactions.Any(candidate => candidate.Id == reaction.Id) &&
            requirement.Owner.Equals(SemanticAddress.ForSlice(requirement.Owner.Application, address.Module, address.Path.Add(feature.Name), slice.Name))) ||
            OwnsReaction((address.Module, address.Path.Add(feature.Name)), feature.Features, reaction, requirement));
}
