// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool UsesRecoverySpecifications(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Specifications)
            .Any(specification => specification.ThenNoEvents || specification.WhenRedelivered is not null);

    static void ValidateRecoverySpecificationVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesRecoverySpecifications(application),
            version,
            SemanticVersion.V17,
            "An ESM v17 model must contain an explicit no-event assertion or recovery redelivery.",
            "Explicit no-event assertions and recovery redelivery require ESM v17.");

    private sealed partial class ValidationContext
    {
        static bool HasPositiveOutcome(SemanticSpecification specification)
        {
            if (specification.ThenNoEvents) return true;

            return specification.ThenReturns is not null || specification.ThenEvents.Length > 0 || !specification.ThenInvocations.IsDefaultOrEmpty || HasReadModelOutcome(specification);
        }

        static bool AssertsActionOutcome(SemanticSpecification specification) =>
            specification.ThenNoEvents || specification.ThenErrors.Length > 0 || specification.ThenEvents.Length > 0 || !specification.ThenInvocations.IsDefaultOrEmpty;

        static bool AssertsMoreThanReadState(SemanticSpecification specification, bool deniedQuery)
        {
            if (AssertsActionOutcome(specification) && !IsQueryRejection(specification)) return true;
            if (specification.ThenDenied && !deniedQuery) return true;

            return !HasReadModelOutcome(specification) && specification.ThenQueries.Length == 0;
        }

        static void ValidateNoEvents(SemanticSpecification specification)
        {
            if (!specification.ThenNoEvents) return;
            if (HasConflictingNoEventsOutcome(specification))
            {
                throw new InvalidSemanticContract("An explicit no-event assertion cannot accompany event, event-order, error or denial expectations.");
            }
        }

        static bool HasConflictingNoEventsOutcome(SemanticSpecification specification)
        {
            if (specification.ThenEvents.Length > 0 || specification.ThenEventsInAnyOrder) return true;

            return specification.ThenErrors.Length > 0 || specification.ThenDenied;
        }

        void ValidateRecoverySpecification(SemanticSpecification specification)
        {
            ValidateNoEvents(specification);
            ValidateRedelivery(specification);
        }

        void ValidateRedelivery(SemanticSpecification specification)
        {
            if (specification.WhenRedelivered is not { } action) return;
            if (action.Given < 0 || action.Given >= specification.GivenEvents.Length)
            {
                throw new InvalidSemanticContract("A redelivery must locate an existing given event by its zero-based position.");
            }

            var reaction = _reactions.SingleOrDefault(reaction => reaction.Id == action.Reaction);
            var fact = specification.GivenEvents[action.Given];
            if (reaction?.Triggers.Any(trigger => trigger.Kind == SemanticReactionTriggerKind.Event && trigger.Source == fact.EventContract) != true)
            {
                throw new InvalidSemanticContract("A redelivery reaction must exist and observe the located given event.");
            }
        }
    }
}
