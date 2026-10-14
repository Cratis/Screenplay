// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static bool HasSpecificationAction(SpecificationSyntax specification) =>
            new object?[] { specification.When, specification.WhenAppended, specification.WhenClock, specification.WhenTrigger, specification.WhenCapture, specification.WhenRedelivered }
                .Any(action => action is not null);

        static bool RequiresSpecificationAction(SpecificationSyntax specification, bool deniedQuery)
        {
            // A query rejection (decision 0072) asserts its error against the query, not against an action.
            if (IsRejectedQuery(specification)) return false;
            if (AssertsActionOutcome(specification)) return true;
            if (specification.ThenDenied is not null && !deniedQuery) return true;

            return !AssertsReadState(specification);
        }

        static bool IsRejectedQuery(SpecificationSyntax specification) =>
            specification.ThenQueries.Count() == 1 && !specification.ThenQueries.Single().Results.Any() && specification.ThenErrors.Count() == 1;

        static bool AssertsActionOutcome(SpecificationSyntax specification) =>
            specification.ThenNoEvents || specification.ThenEvents.Any() || specification.ThenErrors.Any();

        static bool AssertsReadState(SpecificationSyntax specification) =>
            (specification.ThenReadModels?.Any() ?? false) || specification.ThenAbsentReadModels.Any() || specification.ThenQueries.Any();

        static bool HasConflictingNoEventsOutcome(SpecificationSyntax specification) =>
            specification.ThenEvents.Any() || specification.ThenEventsInAnyOrder || HasRejectedOutcome(specification);

        static bool HasRejectedOutcome(SpecificationSyntax specification) =>
            specification.ThenErrors.Any() || specification.ThenDenied is not null;

        static bool ObservesRedeliveredEvent(
            (ReactionSyntax Node, DeclarationScope Scope) reaction,
            EventSyntax? @event,
            ConsistencyDeclarations declarations)
        {
            if (@event is null) return false;

            return reaction.Node.Triggers.Any(trigger => trigger.Source is NamedTriggerSourceSyntax named &&
                ReferenceEquals(declarations.Event(named.Name, reaction.Scope), @event));
        }

        bool ValidateNoEventsExpectation(SpecificationSyntax specification)
        {
            if (!specification.ThenNoEvents) return true;
            if (!HasConflictingNoEventsOutcome(specification)) return true;

            Error(
                DiagnosticCodes.InvalidNoEventsExpectation,
                "'then no events' cannot accompany event, event-order, error or denial expectations.",
                specification.DirectiveLocations.GetValueOrDefault("then no events", specification.Location));

            return false;
        }

        SemanticSpecificationRedelivery? BindRedelivery(SemanticAddress slice, SpecificationSyntax specification)
        {
            if (specification.WhenRedelivered is not { } action) return null;
            var declarations = new ConsistencyDeclarations(syntax, [.. ScreenplayValidator.ScopedSlices(syntax)]);
            var scope = new DeclarationScope([.. slice.Parts.Where(part => part.Kind is SemanticAddressPartKind.Module or SemanticAddressPartKind.Feature or SemanticAddressPartKind.Slice).Select(part => part.Key)]);
            var @event = declarations.Event(action.EventType, scope);
            var reaction = declarations.Resolve(action.Reaction, scope, owner => owner.Reactions, node => node.Name);
            if (reaction is not { } resolved || !ObservesRedeliveredEvent(resolved, @event, declarations))
            {
                Error(DiagnosticCodes.UnknownRedeliveryReaction, $"Reaction '{action.Reaction}' must resolve unambiguously and observe event '{action.EventType}'.", action.Location);
                return null;
            }

            var candidates = specification.Given.Select((given, index) => (Given: given, Index: index))
                .Where(item => ReferenceEquals(declarations.Event(item.Given.EventType, scope), @event))
                .Select(item => (item.Index, Matches: SpecificationRedeliveryValidator.Matches(item.Given, action, @event!, declarations, syntax))).ToArray();
            var matches = candidates.Where(item => item.Matches == true).ToArray();
            if (candidates.Any(item => item.Matches is null) || matches.Length != 1)
            {
                Error(DiagnosticCodes.UnmatchedRedeliveredOccurrence, $"Redelivery of '{action.EventType}' must identify exactly one decidable given occurrence.", action.Location);
                return null;
            }

            var parts = resolved.Scope.Segments;
            var owner = SemanticAddress.ForSlice(_applicationIdentity, parts[0], [.. parts.Skip(1).SkipLast(1)], parts[^1]);
            var address = SemanticAddress.ForReaction(owner, resolved.Node.Name);
            return new(documents.IdentityCatalog.ResolveSemanticAssignment(address).Id, matches[0].Index);
        }
    }
}
