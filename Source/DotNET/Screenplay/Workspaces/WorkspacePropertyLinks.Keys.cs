// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static IEnumerable<ExpressionSyntax> KeyExpressions(KeySyntax? key) => key switch
    {
        ExpressionKeySyntax expression => [expression.Expression],
        CompositeKeySyntax composite => composite.Parts.Select(part => part.Expression),
        _ => []
    };

    static bool UnsupportedKey(SyntaxNode node) => node switch
    {
        RawExpressionSyntax => true,
        SourceItemExpressionSyntax => true,
        ContextExpressionSyntax => true,
        _ => false
    };

    void IndexTypedReader(WorkspaceSyntaxEntry entry)
    {
        IndexNamedReader(entry);
        if (entry.Node is ReadsSyntax { By: { } by })
        {
            Add(entry, "key", new(by, SourceMembers(entry, by), Incomplete: true));
        }
        if (entry.Node is ReadsSyntax reads) IndexReadKeyParts(entry, reads);
        if (entry.Node is QueryParameterSyntax parameter && Owner(entry)?.Syntax is QuerySyntax query)
        {
            var role = query.Filters.Contains(parameter) ? "condition" : "key";
            var models = Resolve(query.ReturnType.Name, ["ReadModel"], entry);
            Add(entry, role, new(parameter.Name, Members(models, parameter.Name), Incomplete: parameter.Name.Contains('.')));
        }
    }

    void IndexReadKeyParts(WorkspaceSyntaxEntry entry, ReadsSyntax reads)
    {
        var models = Resolve(reads.ReadModel, ["ReadModel"], entry);
        foreach (var part in reads.ByParts)
        {
            var use = _entries.First(item => ReferenceEquals(item.Node, part));
            Add(use, "key", new(part.Property, Members(models, part.Property), Incomplete: true));
            foreach (var source in ExpressionSources(part.Source, use))
            {
                Add(source.Entry, "key", new(source.Path, source.Candidates, Incomplete: true));
            }
        }
    }

    void IndexProjectionKeys(ProjectionBlockSyntax block, PropertyProjectionScope scope)
    {
        var nested = block switch
        {
            ChildrenSyntax child => child.Property,
            NestedSyntax value => value.Property,
            _ => null
        };
        if (nested is not null) Add(ProjectionUse(scope, block), "mapping-target", new(nested, Members(scope.Models, scope.Nested ?? nested), Incomplete: true));
        switch (block)
        {
            case JoinSyntax join:
                Add(ProjectionUse(scope, join), "key", new(join.On, Members(scope.Models, scope.Nested ?? join.On), Incomplete: scope.Nested is not null || join.On.Contains('.')));
                break;
            case RemoveWithSyntax remove:
                IndexEventKeys(Resolve(remove.Event, ["Event"], scope.Entry), [remove.Key, remove.ParentKey]);
                break;
            case RemoveViaJoinSyntax remove:
                IndexEventKeys(Resolve(remove.Event, ["Event"], scope.Entry), [remove.Key]);
                break;
        }
    }

    void IndexFromKeys(PropertyProjectionScope scope, FromSyntax from)
    {
        var events = from.Events.SelectMany(spec => Resolve(spec.Event, ["Event"], scope.Entry)).Distinct().ToArray();
        IndexEventKeys(events, KeyExpressions(from.Key).Concat([from.ParentKey]));
        foreach (var spec in from.Events)
        {
            IndexEventKeys(Resolve(spec.Event, ["Event"], scope.Entry), [spec.Key]);
        }
    }

    void IndexEventKeys(AuthoredDeclaration[] events, IEnumerable<ExpressionSyntax?> expressions)
    {
        foreach (var expression in expressions.OfType<ExpressionSyntax>())
        {
            foreach (var path in ProjectionPaths(expression))
            {
                var text = ((PathExpressionSyntax)path.Node).Path;
                Add(path, "key", new(text, Members(events, text), Incomplete: events.Length != 1 || text.Contains('.')));
            }
            foreach (var unsupported in ExpressionEntries(expression).Where(entry => UnsupportedKey(entry.Node)))
            {
                IndexUnsupportedKey(unsupported, events);
            }
        }
    }

    void IndexUnsupportedKey(WorkspaceSyntaxEntry entry, AuthoredDeclaration[] events)
    {
        var text = entry.Node switch
        {
            RawExpressionSyntax raw => raw.Text,
            SourceItemExpressionSyntax source => $"$.{source.Path}",
            ContextExpressionSyntax context => $"$context.{context.Path}",
            _ => string.Empty
        };
        var candidates = entry.Node is SourceItemExpressionSyntax item ? Members(events, item.Path) :
            [.. events.SelectMany(owner => _properties.GetValueOrDefault(owner) ?? [])
                .Where(property => NamesProperty(WorkspaceOpaqueText.Texts(entry.Node) ?? [], property.Name))];
        Add(entry, "key", new(text, candidates, Incomplete: true));
    }

    IEnumerable<WorkspaceSyntaxEntry> ExpressionEntries(ExpressionSyntax expression) => _entries.Where(entry =>
        ReferenceEquals(entry.Node, expression) || Parents(entry).Any(parent => ReferenceEquals(parent.Node, expression)));
}
