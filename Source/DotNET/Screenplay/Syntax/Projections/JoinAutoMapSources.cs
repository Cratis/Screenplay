// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

/// <summary>
/// Identifies event fields consumed by explicit join mappings, which Chronicle excludes from join AutoMap.
/// </summary>
internal static class JoinAutoMapSources
{
    /// <summary>
    /// Gets the explicitly consumed event property paths, ignoring case as Chronicle does.
    /// </summary>
    /// <param name="mappings">The mappings on a joined event.</param>
    /// <returns>The explicitly consumed source paths.</returns>
    internal static HashSet<string> ExplicitlyMapped(IEnumerable<MappingSyntax> mappings) =>
        mappings.OfType<SetMappingSyntax>()
            .Select(mapping => mapping.Source)
            .OfType<PathExpressionSyntax>()
            .Select(source => source.Path)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}
