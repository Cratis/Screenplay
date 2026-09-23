// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

/// <summary>
/// Checks the facts a command would append against the plan's append-time constraints, the way Chronicle checks
/// an append before it commits.
/// </summary>
/// <remarks>
/// <para>
/// Each fact is checked in append order against the existing world plus the facts appended before it in the same
/// command, mirroring Chronicle's per-append batch claims (Chronicle <c>Kernel/Core/Events/Constraints/ConstraintBatchClaims.cs:38-92</c>).
/// One violating fact rejects the whole command, as one violating event fails the whole append (Chronicle
/// <c>Kernel/Core/EventSequences/EventSequence.cs:342-350</c>).
/// </para>
/// <para>
/// A fact with a null event source is attributed to the event source of the fact being checked: specifications
/// replay their <c>given</c> events onto the event source under test, and ESM v1 carries no explicit event source
/// for them.
/// </para>
/// </remarks>
static class SemanticConstraintEnforcement
{
    /// <summary>
    /// Finds the first constraint the facts would violate.
    /// </summary>
    /// <param name="plan">The execution plan carrying the constraints.</param>
    /// <param name="world">The world the facts would be appended to.</param>
    /// <param name="facts">The facts, in append order.</param>
    /// <returns>The violated constraint, or <see langword="null"/> when every constraint holds.</returns>
    internal static SemanticConstraint? FindViolation(
        SemanticExecutionPlan plan,
        SemanticWorld world,
        ImmutableArray<SemanticFact> facts)
    {
        var constraints = plan.Constraints.Values.OrderBy(_ => _.Name, StringComparer.Ordinal).ToArray();
        for (var index = 0; index < facts.Length; index++)
        {
            var candidate = facts[index];
            var history = world.Facts.AddRange(facts.Take(index));
            foreach (var constraint in constraints.Where(_ => Target(_, candidate.EventContract) is not null))
            {
                var violated = constraint.Kind switch
                {
                    SemanticConstraintKind.UniquePropertyValue => ViolatesUniqueValue(constraint, candidate, history),
                    SemanticConstraintKind.UniqueEventOccurrence => ViolatesUniqueOccurrence(constraint, candidate, history),
                    _ => throw new InvalidSemanticContract($"Constraint '{constraint.Name}' kind '{constraint.Kind}' has no evaluator.")
                };

                if (violated)
                {
                    return constraint;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the message a violation reports.
    /// </summary>
    /// <param name="constraint">The violated constraint.</param>
    /// <returns>The declared message, or a default that names the constraint and never the colliding value.</returns>
    internal static string MessageFor(SemanticConstraint constraint) => constraint.Message ?? constraint.Kind switch
    {
        SemanticConstraintKind.UniquePropertyValue => $"Constraint '{constraint.Name}' is violated: another event source already holds the constrained value.",
        _ => $"Constraint '{constraint.Name}' is violated: the event source already has the constrained event."
    };

    // An event source holds at most one claim per constraint: a newer constrained event replaces its claim, so it
    // may claim its own value again or change it (Chronicle Kernel/Storage.MongoDB/Events/Constraints/UniqueConstraintsStorage.cs:40,67-70).
    // A releasing event drops the claim (Chronicle Kernel/Core/Events/Constraints/UniqueConstraintIndexUpdater.cs:28-31).
    static bool ViolatesUniqueValue(SemanticConstraint constraint, SemanticFact candidate, ImmutableArray<SemanticFact> history)
    {
        var value = ConstrainedValue(constraint, candidate);
        if (value.IsEmpty)
        {
            return false;
        }

        var claims = new List<(SemanticValue Owner, ImmutableArray<SemanticValue> Value)>();
        foreach (var fact in history)
        {
            var owner = SourceOf(fact, candidate.Destination);
            var releases = constraint.ReleasedBy.Contains(fact.EventContract);
            if (!releases && Target(constraint, fact.EventContract) is null)
            {
                continue;
            }

            claims.RemoveAll(_ => SemanticValueRules.AreEqual(_.Owner, owner));
            var claimed = releases ? [] : ConstrainedValue(constraint, fact);
            if (!claimed.IsEmpty)
            {
                claims.Add((owner, claimed));
            }
        }

        return claims.Exists(claim =>
            !SemanticValueRules.AreEqual(claim.Owner, candidate.Destination) &&
            SameValue(claim.Value, value, constraint.IgnoreCasing));
    }

    // The event source's own stream decides: a constrained event is open until a releasing event follows it
    // (Chronicle Kernel/Storage.MongoDB/Events/Constraints/UniqueEventTypesConstraintsStorage.cs:44-65).
    static bool ViolatesUniqueOccurrence(SemanticConstraint constraint, SemanticFact candidate, ImmutableArray<SemanticFact> history)
    {
        var open = false;
        foreach (var fact in history.Where(_ => SemanticValueRules.AreEqual(SourceOf(_, candidate.Destination), candidate.Destination)))
        {
            if (constraint.ReleasedBy.Contains(fact.EventContract))
            {
                open = false;
            }
            else if (Target(constraint, fact.EventContract) is not null)
            {
                open = true;
            }
        }

        return open;
    }

    // Null values are skipped, and a fact whose constrained values are all null claims and checks nothing
    // (Chronicle Kernel/Core/Events/Constraints/UniqueConstraintDefinitionExtensions.cs:32-40, UniqueConstraintValidator.cs:32-36).
    static ImmutableArray<SemanticValue> ConstrainedValue(SemanticConstraint constraint, SemanticFact fact) =>
    [
        .. Target(constraint, fact.EventContract)!.Properties
            .Select(property => fact.Values.FirstOrDefault(_ => _.TargetProperty == property)?.Value)
            .Where(_ => _ is not null and not SemanticNullValue)
            .Select(_ => _!)
    ];

    // Ignoring casing compares text values in invariant lower case, as Chronicle does before hashing
    // (Chronicle Kernel/Core/Events/Constraints/UniqueConstraintDefinitionExtensions.cs:53-61).
    static bool SameValue(ImmutableArray<SemanticValue> left, ImmutableArray<SemanticValue> right, bool ignoreCasing) =>
        left.Length == right.Length && left.Zip(right).All(pair => (pair.First, pair.Second, ignoreCasing) switch
        {
            (SemanticTextValue first, SemanticTextValue second, true) =>
                string.Equals(first.Value.ToLowerInvariant(), second.Value.ToLowerInvariant(), StringComparison.Ordinal),
            _ => SemanticValueRules.AreEqual(pair.First, pair.Second)
        });

    static SemanticConstraintTarget? Target(SemanticConstraint constraint, SemanticId eventContract) =>
        constraint.Targets.FirstOrDefault(_ => _.EventContract == eventContract);

    static SemanticValue SourceOf(SemanticFact fact, SemanticValue candidate) =>
        fact.Destination is SemanticNullValue ? candidate : fact.Destination;
}
