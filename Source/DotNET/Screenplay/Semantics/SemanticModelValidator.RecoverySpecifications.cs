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
        void ValidateRecoverySpecification(SemanticSpecification specification)
        {
            if (specification.ThenNoEvents && (specification.ThenEvents.Length > 0 || specification.ThenEventsInAnyOrder ||
                specification.ThenErrors.Length > 0 || specification.ThenDenied))
            {
                throw new InvalidSemanticContract("An explicit no-event assertion cannot accompany event, event-order, error or denial expectations.");
            }

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
