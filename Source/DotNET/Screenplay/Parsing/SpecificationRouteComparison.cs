// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Parsing;

internal static class SpecificationRouteComparison
{
    internal static bool? Matches(SpecificationStreamSyntax? given, SpecificationStreamSyntax? expected, SpecificationNoStreamSyntax? noStream, ApplicationSyntax application)
    {
        if (noStream is not null) return given is null;
        if (expected is null) return true;
        if (given is null) return false;
        var catalog = new EventSourceCatalog(application);
        var actual = catalog.Resolve(given.EventSource, given.Stream);
        var target = catalog.Resolve(expected.EventSource, expected.Stream);
        if (actual.Kind != EventSourceResolutionKind.Unique || target.Kind != EventSourceResolutionKind.Unique) return null;
        if (!ReferenceEquals(actual.Sources[0], target.Sources[0]) || !ReferenceEquals(actual.Streams[0], target.Streams[0])) return false;
        var stream = target.Streams[0];
        var values = new ResponseValueTypes(application);
        if (stream.StreamIdParts.Any())
        {
            if (given.StreamId is not null || expected.StreamId is not null || given.StreamIdParts.Count() != stream.StreamIdParts.Count() || expected.StreamIdParts.Count() != stream.StreamIdParts.Count()) return null;
            var comparisons = new List<bool?>();
            foreach (var part in stream.StreamIdParts)
            {
                var left = given.StreamIdParts.Where(mapping => mapping.Property == part.Name).ToArray();
                var right = expected.StreamIdParts.Where(mapping => mapping.Property == part.Name).ToArray();
                comparisons.Add(left is [var first] && right is [var second] ? Compare(first.Source, second.Source, part.Type, application, values) : null);
            }

            return comparisons.Contains(false) ? false : comparisons.Contains(null) ? null : true;
        }
        if (given.StreamIdParts.Any() || expected.StreamIdParts.Any()) return null;
        if (stream.StreamId is null) return given.StreamId is null && expected.StreamId is null ? true : null;

        return Compare(given.StreamId?.Source, expected.StreamId?.Source, stream.StreamId, application, values);
    }

    static bool? Compare(ExpressionSyntax? left, ExpressionSyntax? right, TypeRefSyntax type, ApplicationSyntax application, ResponseValueTypes values)
    {
        var first = SpecificationStreamValidator.FormatStreamId(left, type, application, values);
        var second = SpecificationStreamValidator.FormatStreamId(right, type, application, values);

        return first is null || second is null ? null : first == second;
    }
}
