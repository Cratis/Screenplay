// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    private sealed partial class ValidationContext
    {
        // The name is a constraint's identity across the whole event store, not just its slice - Chronicle rejects
        // two constraints with one name (Chronicle Clients/DotNET/Events/Constraints/ConstraintBuilder.cs:151-162).
        readonly HashSet<string> _constraintNames = new(StringComparer.Ordinal);

        void RegisterConstraints(SemanticSlice slice)
        {
            RequireObjects(slice.Constraints, nameof(slice.Constraints), "constraint");
            foreach (var constraint in slice.Constraints)
            {
                RequireName(constraint.Name, "constraint");
                if (!_constraintNames.Add(constraint.Name))
                {
                    throw new InvalidSemanticContract($"Duplicate constraint name '{constraint.Name}' is ambiguous.");
                }
            }
        }

        void ValidateConstraints(SemanticSlice slice)
        {
            foreach (var constraint in slice.Constraints)
            {
                ValidateConstraint(constraint);
            }
        }

        void ValidateConstraint(SemanticConstraint constraint)
        {
            ValidateEnum(constraint.Kind, SemanticConstraintKind.Unknown, "constraint kind");
            ValidateEnum(constraint.Scope, SemanticConstraintScope.Unknown, "constraint scope");
            RequireObjects(constraint.Targets, nameof(constraint.Targets), "constraint target");
            RequireArray(constraint.ReleasedBy, nameof(constraint.ReleasedBy));
            if (constraint.Targets.IsEmpty)
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' must constrain at least one event.");
            }

            var constrained = new HashSet<SemanticId>();
            foreach (var target in constraint.Targets)
            {
                ValidateConstraintTarget(constraint, target);
                if (!constrained.Add(target.EventContract))
                {
                    throw new InvalidSemanticContract($"Constraint '{constraint.Name}' constrains event '{target.EventContract}' more than once.");
                }
            }

            var releasing = new HashSet<SemanticId>();
            foreach (var release in constraint.ReleasedBy)
            {
                if (!_events.ContainsKey(release))
                {
                    throw new InvalidSemanticContract($"Constraint '{constraint.Name}' releasing event '{release}' is unresolved.");
                }

                // An event that both claims and releases would leave the claim's outcome to evaluation order.
                if (constrained.Contains(release) || !releasing.Add(release))
                {
                    throw new InvalidSemanticContract($"Constraint '{constraint.Name}' releasing event '{release}' is ambiguous.");
                }
            }

            // Chronicle only offers ignore casing on unique property values (Chronicle
            // Kernel/Concepts/Events/Constraints/UniqueConstraintDefinition.cs:28 versus UniqueEventTypeConstraintDefinition.cs:37).
            if (constraint.IgnoreCasing && constraint.Kind != SemanticConstraintKind.UniquePropertyValue)
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' can only ignore casing when it constrains property values.");
            }

            if (constraint.Message is not null)
            {
                RequireName(constraint.Message, "constraint message");
            }
        }

        void ValidateConstraintTarget(SemanticConstraint constraint, SemanticConstraintTarget target)
        {
            RequireArray(target.Properties, nameof(target.Properties));
            if (!_events.TryGetValue(target.EventContract, out var eventContract))
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' event '{target.EventContract}' is unresolved.");
            }

            if (constraint.Kind == SemanticConstraintKind.UniqueEventOccurrence)
            {
                if (!target.Properties.IsEmpty)
                {
                    throw new InvalidSemanticContract($"Constraint '{constraint.Name}' constrains event occurrences and cannot name properties.");
                }

                return;
            }

            if (target.Properties.IsEmpty)
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' must name at least one property of event '{eventContract.Name}'.");
            }

            var properties = Properties(eventContract.Properties);
            if (target.Properties.Any(property => !properties.ContainsKey(property)))
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' names a property that event '{eventContract.Name}' does not declare.");
            }

            if (target.Properties.Distinct().Count() != target.Properties.Length)
            {
                throw new InvalidSemanticContract($"Constraint '{constraint.Name}' names a property of event '{eventContract.Name}' more than once.");
            }
        }
    }
}
