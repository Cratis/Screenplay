// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
        requirement.Owner.Kind == SemanticKind.Slice &&
        SemanticModelProvenance.Declares(plan.Model, requirement.Owner, requirement.Source.SemanticId) &&
        AllSlices(plan.Model.Application).Any(slice => slice.Id == requirement.Source.SemanticId && slice.Reactions.Any(candidate => candidate.Id == reaction.Id));

    static IEnumerable<SemanticSlice> AllSlices(SemanticApplication application) =>
        application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices);

    static IEnumerable<SemanticSlice> AllSlices(SemanticFeature feature) =>
        feature.Slices.Concat(feature.Features.SelectMany(AllSlices));
}
