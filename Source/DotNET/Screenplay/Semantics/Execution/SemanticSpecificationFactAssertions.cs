// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution;

internal static class SemanticSpecificationFactAssertions
{
    internal static bool ContextMatches(SemanticSpecificationEvent expected, SemanticFact actual) =>
        TagsMatch(expected, actual) && SourceMatches(expected, actual);

    static bool TagsMatch(SemanticSpecificationEvent expected, SemanticFact actual)
    {
        if (expected.ExpectedTags is not { IsDefault: false } tags) return true;
        return tags.SequenceEqual(actual.Tags.IsDefault ? [] : actual.Tags, StringComparer.Ordinal);
    }

    static bool SourceMatches(SemanticSpecificationEvent expected, SemanticFact actual) =>
        expected.EventSource is null ||
        (actual.Context?.EventSource.Type == expected.EventSource.Type &&
         SemanticValueRules.AreEqual(actual.Destination, expected.EventSource.Value));
}
