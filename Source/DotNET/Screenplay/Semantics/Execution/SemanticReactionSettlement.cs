// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Settles event-triggered reactions deterministically in memory for a live host.
/// </summary>
/// <param name="evaluator">The evaluator invoked commands run through, including any host pre-checks.</param>
public sealed class SemanticReactionSettlement(ISemanticEvaluator evaluator)
{
    /// <summary>
    /// Runs reactions to appended facts and their cascading effects as one all-or-nothing evaluation.
    /// </summary>
    /// <param name="plan">The capability-admitted execution plan.</param>
    /// <param name="world">The immutable input world, which must already contain <paramref name="appended"/>.</param>
    /// <param name="appended">The newly appended facts to observe, in append order.</param>
    /// <param name="occurred">When the facts occurred, or <c>null</c> when no occurrence time is supplied.</param>
    /// <returns>
    /// An accepted result with the settled world and only the facts appended by reactions, in order, without queries
    /// or a response; otherwise the failure with the input world unchanged. The host persists accepted facts itself.
    /// </returns>
    public SemanticExecutionResult Settle(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        IEnumerable<SemanticFact> appended,
        DateTimeOffset? occurred)
    {
        var loop = new SemanticReactionLoop(evaluator, plan, world);
        var failure = loop.Observe(appended, occurred);
        var observedCount = loop.Facts.Length;
        failure ??= loop.Settle();
        if (failure is not null)
        {
            return failure with { World = world };
        }

        return new SemanticAccepted(loop.World, loop.Facts[observedCount..], []);
    }
}
