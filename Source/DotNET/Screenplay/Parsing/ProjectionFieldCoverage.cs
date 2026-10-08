// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Resolves structural field coverage without guessing when AutoMap sources are unknown.
/// </summary>
internal static class ProjectionFieldCoverage
{
    internal static HashSet<string>? Covered(IReadOnlyList<ProjectionBlockSyntax> blocks, IReadOnlyList<PropertySyntax> properties, bool autoMap, IReadOnlyList<EverySyntax> inherited, DeclarationScope scope, ConsistencyDeclarations declarations, bool isNested, bool honorJoinAutoMap = false)
    {
        var mapped = new HashSet<string>(StringComparer.Ordinal);
        var every = inherited.Concat(blocks.OfType<EverySyntax>()).ToList();
        foreach (var mapping in every.SelectMany(item => item.Mappings))
        {
            mapped.Add(Root(mapping.Property));
        }

        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var mapping in from.Mappings)
                    {
                        mapped.Add(Root(mapping.Property));
                    }

                    if (autoMap || every.Exists(item => Enabled(item.AutoMap, autoMap)))
                    {
                        foreach (var source in from.Events)
                        {
                            var eventType = declarations.Event(source.Event, scope);
                            if (eventType is null)
                            {
                                return null;
                            }

                            foreach (var field in properties.Where(field => eventType.Properties.Any(sourceField =>
                                sourceField.Name == field.Name && declarations.Compatible(sourceField.Type, field.Type) != false)))
                            {
                                mapped.Add(field.Name);
                            }
                        }
                    }

                    break;
                case AllSyntax all:
                    if (Enabled(all.AutoMap, autoMap))
                    {
                        return null;
                    }

                    foreach (var mapping in all.Mappings)
                    {
                        mapped.Add(Root(mapping.Property));
                    }

                    break;
                case JoinSyntax join when !isNested:
                    foreach (var joined in join.Events)
                    {
                        foreach (var mapping in joined.Mappings)
                        {
                            mapped.Add(Root(mapping.Property));
                        }

                        if (honorJoinAutoMap ? Enabled(joined.AutoMap, autoMap) : autoMap)
                        {
                            var eventType = declarations.Event(joined.Event, scope);
                            if (eventType is null)
                            {
                                return null;
                            }

                            var mappedTargets = joined.Mappings.Select(mapping => mapping.Property.Split('.')[^1]).ToHashSet(StringComparer.OrdinalIgnoreCase);
                            var usedSources = JoinAutoMapSources.ExplicitlyMapped(joined.Mappings);
                            foreach (var field in properties.Where(field => !mappedTargets.Contains(field.Name) && eventType.Properties.Any(sourceField =>
                                sourceField.Name == field.Name && !usedSources.Contains(sourceField.Name) && declarations.Compatible(sourceField.Type, field.Type) != false)))
                            {
                                mapped.Add(field.Name);
                            }
                        }
                    }

                    break;
                case ChildrenSyntax children:
                    mapped.Add(Root(children.Property));
                    break;
                case NestedSyntax nested:
                    mapped.Add(Root(nested.Property));
                    break;
            }
        }

        return mapped;
    }

    internal static bool Enabled(AutoMapMode mode, bool inherited) => mode switch
    {
        AutoMapMode.Enabled => true,
        AutoMapMode.Disabled => false,
        _ => inherited
    };

    internal static string Root(string path) => path.Split('.')[0];
}
