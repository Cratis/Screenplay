// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool IsQueryRejection(SemanticSpecification specification) =>
        !HasAction(specification) && HasOneQueryWithoutResults(specification) &&
        specification.ThenErrors.Length == 1 && !HasOtherOutcome(specification);

    internal static bool UsesQueryRejections(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Specifications).Any(IsQueryRejection);

    static bool HasAction(SemanticSpecification specification) =>
        new object?[] { specification.When, specification.WhenAppended, specification.WhenClock, specification.WhenTrigger, specification.WhenCapture, specification.WhenRedelivered }
            .Any(action => action is not null);

    static bool HasOneQueryWithoutResults(SemanticSpecification specification) =>
        specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty;

    static bool HasOtherOutcome(SemanticSpecification specification) =>
        new[]
        {
            specification.ThenDenied,
            specification.ThenNoEvents,
            specification.ThenEventsInAnyOrder,
            !specification.ThenEvents.IsEmpty,
            !specification.ThenReadModels.IsEmpty,
            !specification.ThenAbsentReadModels.IsEmpty,
            specification.ThenReturns is not null
        }.Any(outcome => outcome);

    static void ValidateQueryRejectionVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesQueryRejections(application),
            version,
            SemanticVersion.V18,
            "An ESM v18 model must contain a query validation rejection specification.",
            "Query validation rejection specifications require ESM v18.");
}
