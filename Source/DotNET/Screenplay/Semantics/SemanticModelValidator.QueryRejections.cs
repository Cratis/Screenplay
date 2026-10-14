// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    internal static bool IsQueryRejection(SemanticSpecification specification) =>
        specification.When is null && specification.WhenAppended is null && specification.WhenClock is null &&
        specification.WhenTrigger is null && specification.WhenCapture is null && specification.WhenRedelivered is null &&
        specification.ThenQueries.Length == 1 && specification.ThenQueries[0].Results.IsEmpty &&
        specification.ThenErrors.Length == 1 && !specification.ThenDenied && !specification.ThenNoEvents &&
        !specification.ThenEventsInAnyOrder && specification.ThenEvents.IsEmpty && specification.ThenReadModels.IsEmpty &&
        specification.ThenAbsentReadModels.IsEmpty && specification.ThenReturns is null;

    internal static bool UsesQueryRejections(SemanticApplication application) =>
        SlicesOf(application).SelectMany(slice => slice.Specifications).Any(IsQueryRejection);

    static void ValidateQueryRejectionVersion(SemanticApplication application, SemanticVersion version) =>
        ValidateFeatureVersion(
            UsesQueryRejections(application),
            version,
            SemanticVersion.V18,
            "An ESM v18 model must contain a query validation rejection specification.",
            "Query validation rejection specifications require ESM v18.");
}
