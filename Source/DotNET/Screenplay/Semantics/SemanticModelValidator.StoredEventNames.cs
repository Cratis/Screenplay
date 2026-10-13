// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateStoredEventNames(SemanticApplication application, SemanticVersion semanticVersion)
    {
        var events = SlicesOf(application).SelectMany(slice => slice.Events);
        var names = new HashSet<string>(StringComparer.Ordinal);
        foreach (var contract in events)
        {
            ValidateStoredName(contract, semanticVersion);

            if (semanticVersion.IsAtLeast(SemanticVersion.V16) && !names.Add(contract.StoredName ?? contract.Name))
            {
                throw new InvalidSemanticContract($"Event stored name '{contract.StoredName ?? contract.Name}' is duplicated.");
            }
        }

        ValidateFeatureVersion(
            events.Any(contract => contract.StoredName is not null && contract.StoredName != contract.Name),
            semanticVersion,
            SemanticVersion.V16,
            "An ESM v16 model must declare a stored event type name that differs from its display name.",
            "Stored event type names require ESM v16.");
    }

    static void ValidateStoredName(SemanticEventContract contract, SemanticVersion semanticVersion)
    {
        if (contract.StoredName is not { } storedName)
        {
            return;
        }

        if (!semanticVersion.IsAtLeast(SemanticVersion.V16))
        {
            throw new InvalidSemanticContract("Stored event type names require ESM v16.");
        }

        if (!StoredEventName.IsValid(storedName))
        {
            throw new InvalidSemanticContract($"Event '{contract.Name}' has an invalid stored name.");
        }
    }
}
