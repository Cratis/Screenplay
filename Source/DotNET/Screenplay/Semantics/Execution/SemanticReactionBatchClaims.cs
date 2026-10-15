// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution;

// Chronicle 19.65.2: persisted property claims and batch reservations are independent. A later
// value change or release cannot free a reservation, whereas event-occurrence cycles can release.
internal sealed class SemanticReactionBatchClaims(SemanticExecutionPlan plan, SemanticWorld world)
{
    readonly SemanticConstraint[] _constraints = [.. plan.Constraints.Values.OrderBy(constraint => constraint.Name, StringComparer.Ordinal)];
    readonly Dictionary<string, List<(SemanticValue Owner, ImmutableArray<SemanticValue> Value)>> _reservations = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<(SemanticValue Owner, bool Open)>> _cycles = new(StringComparer.Ordinal);

    internal SemanticConstraint? Validate(SemanticFact candidate)
    {
        foreach (var constraint in _constraints.Where(constraint => SemanticConstraintEnforcement.Target(constraint, candidate.EventContract) is not null))
        {
            var violated = constraint.Kind switch
            {
                SemanticConstraintKind.UniquePropertyValue => ViolatesProperty(constraint, candidate),
                SemanticConstraintKind.UniqueEventOccurrence => CycleIsOpen(constraint, candidate),
                _ => throw new InvalidSemanticContract($"Constraint '{constraint.Name}' kind '{constraint.Kind}' has no batch evaluator.")
            };
            if (violated) return constraint;
        }

        // Transitions from a rejected event must never affect a subsequent candidate.
        foreach (var constraint in _constraints)
        {
            var targeted = SemanticConstraintEnforcement.Target(constraint, candidate.EventContract) is not null;
            if (constraint.Kind == SemanticConstraintKind.UniquePropertyValue && targeted)
            {
                var value = SemanticConstraintEnforcement.ConstrainedValue(constraint, candidate);
                if (!value.IsEmpty) Reservations(constraint).Add((candidate.Destination, value));
            }
            else if (constraint.Kind == SemanticConstraintKind.UniqueEventOccurrence && (targeted || constraint.ReleasedBy.Contains(candidate.EventContract)))
            {
                var cycles = Cycles(constraint);
                cycles.RemoveAll(claim => SemanticValueRules.AreEqual(claim.Owner, candidate.Destination));
                cycles.Add((candidate.Destination, !constraint.ReleasedBy.Contains(candidate.EventContract)));
            }
        }

        return null;
    }

    bool ViolatesProperty(SemanticConstraint constraint, SemanticFact candidate)
    {
        if (SemanticConstraintEnforcement.ViolatesUniqueValue(constraint, candidate, world.Facts)) return true;
        var value = SemanticConstraintEnforcement.ConstrainedValue(constraint, candidate);
        return !value.IsEmpty && Reservations(constraint).Exists(claim =>
            !SemanticValueRules.AreEqual(claim.Owner, candidate.Destination) &&
            SemanticConstraintEnforcement.SameValue(claim.Value, value, constraint.IgnoreCasing));
    }

    bool CycleIsOpen(SemanticConstraint constraint, SemanticFact candidate)
    {
        var staged = Cycles(constraint).Where(claim => SemanticValueRules.AreEqual(claim.Owner, candidate.Destination)).ToArray();
        return staged.Length > 0 ? staged[0].Open : SemanticConstraintEnforcement.ViolatesUniqueOccurrence(constraint, candidate, world.Facts);
    }

    List<(SemanticValue Owner, ImmutableArray<SemanticValue> Value)> Reservations(SemanticConstraint constraint)
    {
        if (!_reservations.TryGetValue(constraint.Name, out var claims)) _reservations.Add(constraint.Name, claims = []);
        return claims;
    }

    List<(SemanticValue Owner, bool Open)> Cycles(SemanticConstraint constraint)
    {
        if (!_cycles.TryGetValue(constraint.Name, out var claims)) _cycles.Add(constraint.Name, claims = []);
        return claims;
    }
}
