// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Settles event-triggered reactions deterministically in memory for a live host.
/// </summary>
/// <param name="evaluator">The evaluator invoked commands run through, including any host pre-checks.</param>
/// <param name="abilityInvoker">The optional live host invoker. Omit it for deterministic specification behavior.</param>
public sealed class SemanticReactionSettlement(ISemanticEvaluator evaluator, ISemanticAbilityInvoker? abilityInvoker)
{
    /// <summary>
    /// Initializes a settlement using deterministic specification answers and no live host invoker.
    /// </summary>
    /// <param name="evaluator">The evaluator invoked commands run through.</param>
    public SemanticReactionSettlement(ISemanticEvaluator evaluator) : this(evaluator, null)
    {
    }

    /// <summary>
    /// Runs reactions to appended facts and their cascading effects as one all-or-nothing evaluation.
    /// </summary>
    /// <param name="plan">The capability-admitted execution plan.</param>
    /// <param name="world">The immutable input world, which must already contain <paramref name="appended"/> as its last facts.</param>
    /// <param name="appended">The newly appended facts to observe, in append order.</param>
    /// <param name="occurred">When the facts occurred, or <c>null</c> when no occurrence time is supplied.</param>
    /// <returns>
    /// An accepted result with the settled world and only the facts appended by reactions, in order, without queries
    /// or a response; otherwise the failure with the input world unchanged. The host persists accepted facts itself.
    /// </returns>
    /// <remarks>Throws when a live invoker is configured; use <see cref="SettleAsync"/> for host work.</remarks>
    public SemanticExecutionResult Settle(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        IEnumerable<SemanticFact> appended,
        DateTimeOffset? occurred)
    {
        if (abilityInvoker is not null) throw new InvalidOperationException("Use SettleAsync when an ability invoker is configured.");
        var settlement = SettleAsync(plan, world, appended, occurred);
        if (!settlement.IsCompleted) throw new InvalidOperationException("Synchronous settlement must complete without awaiting host work.");
        return settlement.GetAwaiter().GetResult();
    }

    /// <summary>
    /// Settles reactions while awaiting live host ability answers, without blocking on asynchronous host work.
    /// </summary>
    /// <param name="plan">The capability-admitted execution plan.</param>
    /// <param name="world">The world containing the appended facts as its suffix and the complete log prefix for stable identities.</param>
    /// <param name="appended">The newly appended facts, enumerated once.</param>
    /// <param name="occurred">When the facts occurred.</param>
    /// <param name="cancellationToken">The cancellation token, forwarded to the invoker.</param>
    /// <returns>The accepted batch, including pending invocations, or a failure retaining the input world.</returns>
    public async ValueTask<SemanticExecutionResult> SettleAsync(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        IEnumerable<SemanticFact> appended,
        DateTimeOffset? occurred,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var observed = appended.ToArray();
        var loop = CreateLoop(plan, world);

        // A capture reacts to what changed since it last saw the key, so the facts already in the world are what it has seen.
        var failure = loop.FoldCaptures(Math.Max(0, world.Facts.Length - observed.Length)) ?? loop.Observe(observed, occurred);
        var observedCount = loop.Facts.Length;
        failure ??= await loop.SettleAsync(cancellationToken).ConfigureAwait(false);
        return failure is not null ? failure with { World = world } : AcceptedResult(loop, observedCount);
    }

    /// <summary>
    /// Resumes one persisted pending invocation from its selected answer branch, without invoking its ability again.
    /// </summary>
    /// <param name="plan">The plan used for the original settlement.</param>
    /// <param name="world">The current world with the complete log prefix.</param>
    /// <param name="pending">The persisted pending invocation.</param>
    /// <param name="answer">The returned values, named outcome, or failure supplied later by the host.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The branch and cascade's new facts and pending invocations, or a failure retaining the input world.</returns>
    /// <remarks>The host must deduplicate answers by identity; applying the same answer twice can append duplicate facts.</remarks>
    public async ValueTask<SemanticExecutionResult> SettleAnswerAsync(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        SemanticPendingAbilityInvocation pending,
        SemanticAbilityAnswer answer,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var loop = CreateLoop(plan, world);
        var failure = loop.FoldCaptures(world.Facts.Length);
        failure ??= await loop.SettleAnswerAsync(pending, answer, cancellationToken).ConfigureAwait(false);
        return failure is not null ? failure with { World = world } : AcceptedResult(loop, 0);
    }

    static SemanticAccepted AcceptedResult(SemanticReactionLoop loop, int observedCount) =>
        new(loop.World, loop.Facts[observedCount..], []) { PendingInvocations = loop.PendingInvocations };

    SemanticReactionLoop CreateLoop(SemanticExecutionPlan plan, SemanticWorld world) => new(evaluator, plan, world) { AbilityInvoker = abilityInvoker };
}
