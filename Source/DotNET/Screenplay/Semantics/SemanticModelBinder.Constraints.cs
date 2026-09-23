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

        // The language declares one event, no releasing events, case-sensitive values, no message and no scope, so
        // each of those binds to the contract's default - Chronicle's default for a constraint declared the same way.
        static SemanticConstraint Constraint(string name, SemanticConstraintKind kind, SemanticConstraintTarget target) =>
            new(name, kind, SemanticConstraintScope.EventSequence, [target], [], false, null);

        ImmutableArray<SemanticConstraint> BindConstraints(SliceSyntax slice) =>
            [.. slice.Constraints.Select(BindConstraint).Where(_ => _ is not null).Select(_ => _!)];

        SemanticConstraint? BindConstraint(ConstraintSyntax constraint)
        {
            var bound = constraint switch
            {
                UniquePropertyConstraintSyntax unique => BindUniqueProperty(unique),
                UniqueEventConstraintSyntax unique => BindUniqueEvent(unique),
                FileConstraintSyntax file => RejectFileConstraint(file),
                _ => RejectUnknownConstraint(constraint)
            };

            if (!_constraintNames.Add(constraint.Name))
            {
                Error(
                    DiagnosticCodes.DuplicateConstraintName,
                    $"Constraint '{constraint.Name}' is declared more than once. A constraint's name is its identity in the event store, so it must be unique across the application.",
                    constraint.Location);
                return null;
            }

            return bound;
        }

        SemanticConstraint? BindUniqueProperty(UniquePropertyConstraintSyntax constraint)
        {
            if (ConstrainedEvent(constraint.Name, constraint.Event, constraint.Location) is not { } @event)
            {
                return null;
            }

            if (constraint.Property.Contains('.', StringComparison.Ordinal))
            {
                Error(
                    DiagnosticCodes.UnsupportedSemanticSyntax,
                    $"Constraint '{constraint.Name}' property path '{constraint.Property}' is not admitted by ESM v1; a unique constraint names a property the event declares directly.",
                    constraint.Location);
                return null;
            }

            if (!@event.Properties.TryGetValue(constraint.Property, out var property))
            {
                Error(
                    DiagnosticCodes.UnknownConstraintProperty,
                    $"Constraint '{constraint.Name}' names property '{constraint.Property}', which event '{constraint.Event}' does not declare.",
                    constraint.Location);
                return null;
            }

            return Constraint(constraint.Name, SemanticConstraintKind.UniquePropertyValue, new(@event.Contract.Id, [property.Id]));
        }

        SemanticConstraint? BindUniqueEvent(UniqueEventConstraintSyntax constraint) =>
            ConstrainedEvent(constraint.Name, constraint.Event, constraint.Location) is { } @event
                ? Constraint(constraint.Name, SemanticConstraintKind.UniqueEventOccurrence, new(@event.Contract.Id, []))
                : null;

        SemanticConstraint? RejectFileConstraint(FileConstraintSyntax constraint)
        {
            Error(
                DiagnosticCodes.UnsupportedSemanticSyntax,
                $"Constraint '{constraint.Name}' file implementation requires a constrained implementation attachment.",
                constraint.Location);
            return null;
        }

        SemanticConstraint? RejectUnknownConstraint(ConstraintSyntax constraint)
        {
            Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Constraint '{constraint.Name}' is not admitted by ESM v1.", constraint.Location);
            return null;
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
