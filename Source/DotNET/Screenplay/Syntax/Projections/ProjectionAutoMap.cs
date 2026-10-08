// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.Projections;

static class ProjectionAutoMap
{
    internal static IEnumerable<(T Target, T Source)> Properties<T>(
        IEnumerable<MappingSyntax> sourceMappings,
        IEnumerable<T> targets,
        IEnumerable<T> sources,
        Func<T, string> name,
        Func<T, T, bool> compatible,
        bool isJoin)
        where T : class
    {
        var mappings = sourceMappings.ToArray();
        var aggregateOnly = mappings.Length > 0 && mappings.All(mapping => mapping is AddMappingSyntax or SubtractMappingSyntax or IncrementMappingSyntax or DecrementMappingSyntax or CountMappingSyntax);
        if (!isJoin && aggregateOnly) yield break;
        var mapped = mappings.Select(mapping => mapping.Property.Split('.')[^1]).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var usedSources = isJoin ? JoinAutoMapSources.ExplicitlyMapped(mappings) : [];
        var targetProperties = targets.ToArray();
        foreach (var source in sources)
        {
            var target = targetProperties.FirstOrDefault(target => string.Equals(name(target), name(source), StringComparison.OrdinalIgnoreCase));
            if (target is null || mapped.Contains(name(target)) || usedSources.Contains(name(source)) || !compatible(target, source)) continue;
            mapped.Add(name(target));
            yield return (target, source);
        }
    }
}
