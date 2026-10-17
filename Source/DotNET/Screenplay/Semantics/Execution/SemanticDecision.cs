// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Holds a command decision without publishing it; only settlement sees the commit-time world.
/// </summary>
/// <remarks>
/// A decision is a value: <see cref="Commit"/> never changes it or the world it receives, so a host may commit the same
/// decision against several worlds in turn, for example after a guarded append lost a race.
/// </remarks>
public sealed class SemanticDecision
{
    readonly SemanticWorld _decidedWorld;
    readonly Func<SemanticWorld, SemanticExecutionResult> _settle;
    readonly bool _refused;

    internal SemanticDecision(
        SemanticWorld decidedWorld,
        SemanticDecisionDependencies dependencies,
        Func<SemanticWorld, SemanticExecutionResult> settle,
        bool refused = false)
    {
        _decidedWorld = decidedWorld;
        Dependencies = dependencies;
        _settle = settle;
        _refused = refused;
    }

    /// <summary>
    /// Gets the boundary: the count of facts in the world the decision was made against.
    /// </summary>
    public int Boundary => _decidedWorld.Facts.Length;

    /// <summary>
    /// Gets the protected state the decision depended on.
    /// </summary>
    public SemanticDecisionDependencies Dependencies { get; }

    /// <summary>
    /// Settles the decision against a commit-time world that extends the decided world.
    /// </summary>
    /// <remarks>
    /// Only a fact after the <see cref="Boundary"/> that matches <see cref="Dependencies"/> conflicts; every other later
    /// fact is settled against, as constraints, projections and publications see the commit-time world.
    /// </remarks>
    /// <param name="world">The commit-time world; it must hold the decided facts as its prefix.</param>
    /// <returns>The accepted, rejected or conflicting result; a conflict carries no facts.</returns>
    public SemanticExecutionResult Commit(SemanticWorld world)
    {
        if (!ReferenceEquals(world, _decidedWorld) &&
            (world.Facts.Length < Boundary || !_decidedWorld.Facts.Zip(world.Facts).All(pair => SameFact(pair.First, pair.Second))))
        {
            return new SemanticRejected(world, SemanticRejectionCategory.Contract, null, "The commit world is not a prefix-extension of the decide world.");
        }

        // A refused decision performs no settlement or conflict checking.
        if (!_refused && world.Facts.Skip(Boundary).Any(Dependencies.Matches))
        {
            return new SemanticConflict(world, SemanticConflictCategory.DecisionStateChanged, "Protected decision state changed after it was read.");
        }

        return _settle(world);
    }

    internal static SemanticDecision Refused(SemanticExecutionResult result) =>
        new(result.World, SemanticDecisionDependencies.Empty, world => result with { World = world }, refused: true);

    static bool SameFact(SemanticFact first, SemanticFact second) =>
        ReferenceEquals(first, second) ||
        (first.EventContract == second.EventContract && SemanticValueRules.AreEqual(first.Destination, second.Destination) &&
         first.Context == second.Context && first.Route == second.Route && first.Occurred == second.Occurred &&
         first.ReactionOrigin == second.ReactionOrigin && first.Tags.SequenceEqual(second.Tags) &&
         first.Values.Length == second.Values.Length && first.Values.Zip(second.Values).All(pair =>
             pair.First.TargetProperty == pair.Second.TargetProperty && SemanticValueRules.AreEqual(pair.First.Value, pair.Second.Value)));
}
