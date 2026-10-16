// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

internal static class SemanticKeyPartValues
{
    internal static ImmutableArray<SemanticPropertyValue> Require(SemanticValue key, IEnumerable<SemanticId> properties)
    {
        var expected = properties.ToHashSet();
        if (key is not SemanticCompositeValue composite || composite.Properties.IsDefault || composite.Properties.Length != expected.Count)
        {
            throw new InvalidSemanticContract("A composite read-model key must supply every named part exactly once.");
        }
        var seen = new HashSet<SemanticId>();
        foreach (var part in composite.Properties)
        {
            if (part is null || part.Value is null || !expected.Contains(part.TargetProperty) || !seen.Add(part.TargetProperty))
            {
                throw new InvalidSemanticContract("A composite read-model key contains an unknown or repeated part.");
            }
        }

        return composite.Properties;
    }
}
