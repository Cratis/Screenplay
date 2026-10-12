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
        foreach (var entry in _entries.Where(entry => entry.Node is ProjectionSyntax)) IndexProjection(entry);
    }

    void IndexProjection(WorkspaceSyntaxEntry entry)
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

    void Project(PropertyProjectionScope scope, IEnumerable<ProjectionBlockSyntax> source)
    {
        var blocks = source.ToArray();
        var every = blocks.OfType<EverySyntax>().ToArray();
        var feeds = scope with { Inherited = scope.Inherited.Concat(every.SelectMany(block => block.Mappings)) };
        var children = scope with { Inherited = scope.Inherited.Concat(every.Where(block => block.IncludeChildren).SelectMany(block => block.Mappings)) };
        foreach (var block in blocks) ProjectBlock(block, feeds, children);
    }

    void ProjectBlock(ProjectionBlockSyntax block, PropertyProjectionScope feeds, PropertyProjectionScope children)
    {
        IndexProjectionKeys(block, feeds);
        var rootAutoMap = ((ProjectionSyntax)feeds.Entry.Node).AutoMap != AutoMapMode.Disabled;
        switch (block)
        {
            case FromSyntax from:
                ProjectFrom(feeds, from);
                break;
            case JoinSyntax join:
                ProjectJoin(feeds, join);
                break;
            case AllSyntax all:
                ProjectAll(feeds, all);
                break;
            case ChildrenSyntax child:
                Project(children with { AutoMap = Mode(child.AutoMap, rootAutoMap), Nested = children.Nested ?? child.Property }, child.Blocks);
                break;
            case NestedSyntax nested:
                Project(children with { AutoMap = Mode(nested.AutoMap, rootAutoMap), Nested = children.Nested ?? nested.Property }, nested.Blocks);
                break;
        }
    }

    void ProjectFrom(PropertyProjectionScope scope, FromSyntax from)
    {
        IndexFromKeys(scope, from);
        foreach (var spec in from.Events)
        {
            Feed(scope, new(Resolve(spec.Event, ["Event"], scope.Entry), from.Mappings, false, spec));
        }
    }

    void ProjectJoin(PropertyProjectionScope scope, JoinSyntax join)
    {
        foreach (var spec in join.Events) Feed(scope with { AutoMap = Mode(spec.AutoMap, scope.AutoMap) }, new(Resolve(spec.Event, ["Event"], scope.Entry), spec.Mappings, true, spec));
    }

    void ProjectAll(PropertyProjectionScope scope, AllSyntax all)
    {
        foreach (var @event in Current(_index.Declarations.Where(declaration => declaration.Kind == "Event")))
        {
            Feed(scope with { AutoMap = Mode(all.AutoMap, scope.AutoMap) }, new([@event], all.Mappings, false, all));
        }
    }

    void Feed(PropertyProjectionScope scope, PropertyProjectionFeed feed)
    {
        var mappings = feed.Mappings.ToArray();
        foreach (var mapping in mappings.Concat(scope.Inherited)) ProjectMapping(scope, feed.Events, mapping, ProjectionUse(scope, feed.Occurrence));
        if (scope.AutoMap) AutoMap(scope, feed with { Mappings = mappings });
    }

    WorkspaceSyntaxEntry ProjectionUse(PropertyProjectionScope scope, SyntaxNode occurrence) =>
        _entries.FirstOrDefault(entry => ReferenceEquals(entry.Node, occurrence)) ?? scope.Entry with { Node = occurrence };

    sealed record PropertyProjectionFeed(AuthoredDeclaration[] Events, IEnumerable<MappingSyntax> Mappings, bool IsJoin, SyntaxNode Occurrence);

    sealed record PropertyProjectionScope(WorkspaceSyntaxEntry Entry, AuthoredDeclaration[] Models, bool AutoMap, IEnumerable<MappingSyntax> Inherited)
    {
        internal string? Nested { get; init; }
    }
}
