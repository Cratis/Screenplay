// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Holds a command decision without publishing it; only settlement sees the commit-time world.
/// </summary>
internal sealed class SemanticDecision(
    SemanticWorld decidedWorld,
    SemanticDecisionDependencies dependencies,
    Func<SemanticWorld, SemanticExecutionResult> settle,
    bool refused = false)
{
    internal int Boundary => decidedWorld.Facts.Length;

    internal SemanticDecisionDependencies Dependencies => dependencies;

    internal static SemanticDecision Refused(SemanticExecutionResult result, SemanticDecisionDependencies? dependencies = null) =>
        new(result.World, dependencies ?? SemanticDecisionDependencies.Empty, world => result with { World = world }, refused: true);

    internal SemanticExecutionResult Commit(SemanticWorld world)
    {
        if (!ReferenceEquals(world, decidedWorld) &&
            (world.Facts.Length < Boundary || !decidedWorld.Facts.Zip(world.Facts).All(pair => SameFact(pair.First, pair.Second))))
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "The commit world is not a prefix-extension of the decide world.");
        }

        // A refused decision performs no settlement, even when its recorded dependencies have changed.
        if (!refused && world.Facts.Skip(Boundary).Any(dependencies.Matches))
        {
            return new SemanticConflict(world, SemanticConflictCategory.DecisionStateChanged, "Protected decision state changed after it was read.");
        }

        return settle(world);
    }

    static bool SameFact(SemanticFact first, SemanticFact second) =>
        ReferenceEquals(first, second) ||
        (first.EventContract == second.EventContract && SemanticValueRules.AreEqual(first.Destination, second.Destination) &&
         first.Context == second.Context && first.Route == second.Route && first.Occurred == second.Occurred &&
         first.ReactionOrigin == second.ReactionOrigin && first.Tags.SequenceEqual(second.Tags) &&
         first.Values.Length == second.Values.Length && first.Values.Zip(second.Values).All(pair =>
             pair.First.TargetProperty == pair.Second.TargetProperty && SemanticValueRules.AreEqual(pair.First.Value, pair.Second.Value)));
}
