// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static bool CompatibleProperties(PropertySyntax target, PropertySyntax source) =>
        target.Type.Name == source.Type.Name && target.Type.IsCollection == source.Type.IsCollection && (!source.Type.IsOptional || target.Type.IsOptional);

    void AutoMap(PropertyProjectionScope scope, PropertyProjectionFeed feed)
    {
        var use = ProjectionUse(scope, feed.Occurrence);
        if (scope.Nested is not null)
        {
            Add(use, "automap", new(scope.Nested, Members(scope.Models, scope.Nested), Incomplete: true));
            IncompleteAutoMap(use, feed.Events);
            return;
        }
        if (feed.Events is not [var @event] || scope.Models is not [var model])
        {
            IncompleteAutoMap(use, feed.Events.Concat(scope.Models));
            return;
        }
        foreach (var pair in ProjectionAutoMap.Properties(feed.Mappings, Properties(model.Syntax), Properties(@event.Syntax), property => property.Name, CompatibleProperties, feed.IsJoin))
        {
            var targets = Members([model], pair.Target.Name);
            var sources = Members([@event], pair.Source.Name);
            _projectionEffects.Add(new(
                use,
                [@event],
                targets,
                new SetMappingSyntax(pair.Target.Name, new PathExpressionSyntax(pair.Source.Name, use.Location), use.Location),
                [new(pair.Source.Name, sources)],
                targets.Length == 1 && sources.Length == 1) { AutoMapped = true });
            Add(use, "automap", new(pair.Target.Name, targets, sources is [var source] ? source : null, sources.Length != 1));
            Add(use, "automap", new(pair.Source.Name, sources, targets is [var target] ? target : null, targets.Length != 1));
        }
    }

    void IncompleteAutoMap(WorkspaceSyntaxEntry use, IEnumerable<AuthoredDeclaration> owners)
    {
        foreach (var property in owners.SelectMany(owner => _properties.GetValueOrDefault(owner) ?? [])) Add(use, "automap", new(property.Name, [property], Incomplete: true));
    }
}
