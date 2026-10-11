// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static bool Mode(AutoMapMode mode, bool inherited) => mode == AutoMapMode.Inherit ? inherited : mode == AutoMapMode.Enabled;

    void IndexProjections()
    {
        foreach (var entry in _entries.Where(entry => entry.Node is ProjectionSyntax))
        {
            var projection = (ProjectionSyntax)entry.Node;
            var shared = projection.Blocks.Where(block => block is not ProjectionVariantSyntax).ToArray();
            var variants = projection.Blocks.OfType<ProjectionVariantSyntax>().ToArray();
            var scope = new PropertyProjectionScope(entry, [], projection.AutoMap != AutoMapMode.Disabled, []);
            if (variants.Length == 0)
            {
                Project(scope with { Models = Resolve(projection.ReadModel ?? projection.Name, ["ReadModel"], entry) }, shared);
            }
            foreach (var variant in variants)
            {
                Project(scope with { Models = Resolve(variant.Name, ["ReadModel"], entry) }, ProjectionVariantHandlers.For(shared, variant));
            }
        }
    }

    void Project(PropertyProjectionScope scope, IEnumerable<ProjectionBlockSyntax> source)
    {
        var blocks = source.ToArray();
        var every = blocks.OfType<EverySyntax>().ToArray();
        var feeds = scope with { Inherited = scope.Inherited.Concat(every.SelectMany(block => block.Mappings)) };
        var children = scope with { Inherited = scope.Inherited.Concat(every.Where(block => block.IncludeChildren).SelectMany(block => block.Mappings)) };
        var rootAutoMap = ((ProjectionSyntax)scope.Entry.Node).AutoMap != AutoMapMode.Disabled;
        foreach (var block in blocks)
        {
            switch (block)
            {
                case FromSyntax from:
                    foreach (var spec in from.Events) Feed(feeds, Resolve(spec.Event, ["Event"], scope.Entry), from.Mappings, false, spec);
                    break;
                case JoinSyntax join:
                    foreach (var spec in join.Events) Feed(feeds with { AutoMap = Mode(spec.AutoMap, scope.AutoMap) }, Resolve(spec.Event, ["Event"], scope.Entry), spec.Mappings, true, spec);
                    break;
                case AllSyntax all:
                    foreach (var @event in Current(_index.Declarations.Where(declaration => declaration.Kind == "Event"))) Feed(feeds with { AutoMap = Mode(all.AutoMap, scope.AutoMap) }, [@event], all.Mappings, false, all);
                    break;
                case ChildrenSyntax child:
                    Project(children with { AutoMap = Mode(child.AutoMap, rootAutoMap), Nested = scope.Nested ?? child.Property }, child.Blocks);
                    break;
                case NestedSyntax nested:
                    Project(children with { AutoMap = Mode(nested.AutoMap, rootAutoMap), Nested = scope.Nested ?? nested.Property }, nested.Blocks);
                    break;
            }
        }
    }

    void Feed(PropertyProjectionScope scope, AuthoredDeclaration[] events, IEnumerable<MappingSyntax> sourceMappings, bool isJoin, SyntaxNode occurrence)
    {
        var mappings = sourceMappings.ToArray();
        foreach (var mapping in mappings.Concat(scope.Inherited))
        {
            var use = _entries.FirstOrDefault(entry => ReferenceEquals(entry.Node, mapping)) ?? scope.Entry with { Node = mapping };
            var targets = Members(scope.Models, scope.Nested ?? mapping.Property);
            var expression = mapping switch
            {
                SetMappingSyntax set => set.Source,
                AddMappingSyntax add => add.Value,
                SubtractMappingSyntax subtract => subtract.Value,
                _ => null
            };
            var paths = expression is null ? [] : _entries.Where(entry => entry.Node is PathExpressionSyntax && (ReferenceEquals(entry.Node, expression) || Parents(entry).Any(parent => ReferenceEquals(parent.Node, expression)))).ToArray();
            var sources = paths.SelectMany(entry => Members(events, ((PathExpressionSyntax)entry.Node).Path)).Distinct().ToArray();
            var incomplete = targets.Length != 1 || events.Length != 1 || scope.Nested is not null || mapping.Property.Contains('.') || paths.Any(entry => Members(events, ((PathExpressionSyntax)entry.Node).Path).Length != 1) || expression is RawExpressionSyntax;
            Add(use, "mapping-target", targets, mapping.Property, sources is [var only] ? only : null, incomplete);
            foreach (var path in paths)
            {
                var name = ((PathExpressionSyntax)path.Node).Path;
                Add(path, "mapping-source", Members(events, name), name, targets is [var target] ? target : null, targets.Length != 1 || events.Length != 1 || scope.Nested is not null || name.Contains('.'));
            }
        }
        if (!scope.AutoMap) return;
        var useSite = _entries.FirstOrDefault(entry => ReferenceEquals(entry.Node, occurrence)) ?? scope.Entry with { Node = occurrence };
        if (scope.Nested is not null)
        {
            Add(useSite, "automap", Members(scope.Models, scope.Nested), scope.Nested, incomplete: true);
            IncompleteAutoMap(useSite, events);
            return;
        }
        if (events is not [var @event] || scope.Models is not [var model])
        {
            IncompleteAutoMap(useSite, events.Concat(scope.Models));
            return;
        }
        foreach (var pair in ProjectionAutoMap.Properties(
            mappings,
            Properties(model.Syntax),
            Properties(@event.Syntax),
            property => property.Name,
            (target, source) => target.Type.Name == source.Type.Name && target.Type.IsCollection == source.Type.IsCollection && (!source.Type.IsOptional || target.Type.IsOptional),
            isJoin))
        {
            var targets = Members([model], pair.Target.Name);
            var sources = Members([@event], pair.Source.Name);
            Add(useSite, "automap", targets, pair.Target.Name, sources is [var source] ? source : null, sources.Length != 1);
            Add(useSite, "automap", sources, pair.Source.Name, targets is [var target] ? target : null, targets.Length != 1);
        }
    }

    void IncompleteAutoMap(WorkspaceSyntaxEntry use, IEnumerable<AuthoredDeclaration> owners)
    {
        foreach (var property in owners.SelectMany(owner => _properties.GetValueOrDefault(owner) ?? [])) Add(use, "automap", [property], property.Name, incomplete: true);
    }

    sealed record PropertyProjectionScope(WorkspaceSyntaxEntry Entry, AuthoredDeclaration[] Models, bool AutoMap, IEnumerable<MappingSyntax> Inherited, string? Nested = null);
}
