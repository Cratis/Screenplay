// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        // The name is the constraint's identity in the event store, so it is claimed across every slice.
        readonly HashSet<string> _constraintNames = new(StringComparer.Ordinal);

        ImmutableArray<SemanticConstraint> BindConstraints(SliceSyntax slice) =>
            [.. slice.Constraints.Select(BindConstraint).Where(_ => _ is not null).Select(_ => _!)];

        SemanticConstraint? BindConstraint(ConstraintSyntax constraint)
        {
            ValidateStringKey(constraint.Message, constraint.Location);
            if (!_constraintNames.Add(constraint.Name))
            {
                Error(
                    DiagnosticCodes.DuplicateConstraintName,
                    $"Constraint '{constraint.Name}' is declared more than once. A constraint's name is its identity in the event store, so it must be unique across the application.",
                    constraint.Location);
                return null;
            }

            if (constraint is FileConstraintSyntax file)
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Constraint '{file.Name}' file implementation is not admitted by the executable model: Chronicle file constraints can only declare uniqueness. Declare it with 'unique ...' for portability; put other rules in command validation or a 'require' condition.",
                    file.Location);
                return null;
            }

            var kind = constraint is UniquePropertyConstraintSyntax
                ? SemanticConstraintKind.UniquePropertyValue
                : SemanticConstraintKind.UniqueEventOccurrence;
            if (constraint.IgnoreCasing && kind is SemanticConstraintKind.UniqueEventOccurrence)
            {
                Error(
                    DiagnosticCodes.InvalidConstraintCasing,
                    $"Constraint '{constraint.Name}' can ignore casing only for unique properties, not unique events.",
                    constraint.Location);
                return null;
            }

            var targets = new List<SemanticConstraintTarget>();
            foreach (var rule in new[] { constraint }.Concat(constraint.AdditionalRules))
            {
                var target = BindConstraintTarget(rule);
                if (target is null)
                {
                    return null;
                }

                targets.Add(target);
            }

            var releasedBy = new List<SemanticId>();
            foreach (var name in constraint.ReleasedBy)
            {
                if (ConstrainedEvent(constraint.Name, name, constraint.Location) is not { } @event)
                {
                    return null;
                }

                releasedBy.Add(@event.Contract.Id);
            }

            return new(
                constraint.Name,
                kind,
                SemanticConstraintScope.EventSequence,
                [.. targets],
                [.. releasedBy],
                constraint.IgnoreCasing,
                constraint.Message);
        }

        SemanticConstraintTarget? BindConstraintTarget(ConstraintSyntax constraint)
        {
            var eventName = constraint switch
            {
                UniquePropertyConstraintSyntax property => property.Event,
                UniqueEventConstraintSyntax occurrence => occurrence.Event,
                _ => null
            };
            if (eventName is null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Constraint '{constraint.Name}' is not admitted by ESM v1.", constraint.Location);
                return null;
            }

            if (ConstrainedEvent(constraint.Name, eventName, constraint.Location) is not { } @event)
            {
                return null;
            }

            if (constraint is not UniquePropertyConstraintSyntax unique)
            {
                return new(@event.Contract.Id, []);
            }

            var properties = new List<SemanticId>();
            foreach (var name in new[] { unique.Property }.Concat(unique.AdditionalProperties))
            {
                if (name.Contains('.', StringComparison.Ordinal))
                {
                    Error(
                        DiagnosticCodes.UnsupportedSemanticSyntax,
                        $"Constraint '{constraint.Name}' property path '{name}' is not admitted by ESM v1; a unique constraint names a property the event declares directly.",
                        constraint.Location);
                    return null;
                }

                if (!@event.Properties.TryGetValue(name, out var property))
                {
                    Error(
                        DiagnosticCodes.UnknownConstraintProperty,
                        $"Constraint '{constraint.Name}' names property '{name}', which event '{eventName}' does not declare.",
                        constraint.Location);
                    return null;
                }

                properties.Add(property.Id);
            }

            return new(@event.Contract.Id, [.. properties]);
        }

        BoundEvent? ConstrainedEvent(string constraint, string name, SourceLocation location)
        {
            if (_events.TryGetValue(name, out var @event))
            {
                return @event;
            }

            Error(
                DiagnosticCodes.UnknownConstraintEvent,
                $"Constraint '{constraint}' names event '{name}', which the application does not declare.",
                location);
            return null;
        }
    }
}
