// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    void IndexReaders()
    {
        foreach (var entry in _entries)
        {
            var path = entry.Node switch
            {
                ComparisonConditionSyntax condition => condition.Left,
                ValidationRuleSyntax validation => validation.Property,
                PathExpressionSyntax expression when Parents(entry).Any(parent => parent.Node is ConditionSyntax or PolicyConditionSyntax) => expression.Path,
                _ => null
            };
            if (path is not null) Add(entry, "condition", SourceMembers(entry, path), path, incomplete: NestedSource(entry, path));
            IndexBinding(entry);
            IndexOpaque(entry);
        }
    }

    void IndexBinding(WorkspaceSyntaxEntry entry)
    {
        var path = entry.Node switch
        {
            ScreenColumnSyntax column => column.Property,
            ScreenFieldSyntax field => field.Property,
            FormFieldSyntax field => field.Property,
            FormColumnSyntax column => column.Property,
            FormFieldPlacementSyntax placement => placement.Field,
            UiBindingSyntax { BindingKind: UiBindingKind.DataContext or UiBindingKind.QueryResult } binding => binding.Path,
            _ => null
        };
        if (string.IsNullOrEmpty(path)) return;
        var ancestors = Parents(entry).ToArray();
        var form = ancestors.Select(parent => parent.Node).OfType<FormSyntax>().FirstOrDefault();
        var owners = form is not null ? Resolve(form.For, ["Command"], entry) : BindingOwners(entry, ancestors);
        var targets = Members(owners, path);
        var from = (entry.Node as FormFieldSyntax)?.From;
        var sources = from is not null && form?.Populate is FormPopulateViaQuerySyntax query ? Members(QueryView(query.Query, entry), from) : [];
        Add(entry, "screen-binding", targets, path, sources is [var source] ? source : null, owners.Length != 1 || path.Contains('.') || (from is not null && sources.Length != 1));
        if (from is not null)
        {
            Add(entry, "screen-binding", sources, from, targets is [var target] ? target : null, targets.Length != 1 || from.Contains('.'));
        }
    }

    AuthoredDeclaration[] BindingOwners(WorkspaceSyntaxEntry entry, WorkspaceSyntaxEntry[] ancestors)
    {
        if (entry.Node is UiBindingSyntax { BindingKind: UiBindingKind.QueryResult, Query: { } query }) return QueryView(query, entry);
        var target = ancestors.Select(parent => parent.Node).Select(node => node switch
        {
            ScreenTableSyntax table => table.Target,
            ScreenSummarySyntax summary => summary.Target,
            _ => null
        }).FirstOrDefault(name => name is not null);
        if (target is not null) return Resolve(target, ["ReadModel"], entry);
        var screen = ancestors.Select(parent => parent.Node).OfType<ScreenSyntax>().FirstOrDefault();
        var data = screen is null ? [] : _entries.Where(item => item.Node is ScreenDataSyntax && Parents(item).Any(parent => ReferenceEquals(parent.Node, screen)))
            .Select(item => (ScreenDataSyntax)item.Node).ToArray();
        return [.. data.SelectMany(source => Resolve(source.Type.Name, ["ReadModel"], entry)).Distinct()];
    }

    AuthoredDeclaration[] QueryView(string query, WorkspaceSyntaxEntry use) => Resolve(query, ["Query"], use) is [var declaration] && declaration.Syntax is QuerySyntax syntax
        ? ResolveAtOwner(syntax.ReturnType.Name, ["ReadModel"], declaration, use.Location) : [];

    void IndexOpaque(WorkspaceSyntaxEntry entry)
    {
        if (entry.Node is not (CodeBlockSyntax or RawExpressionSyntax or FileReferenceSyntax or FormFieldSyntax { ComposeUsing: not null })) return;
        var owner = Owner(entry);
        if (owner is null) return;
        var callers = owner.Syntax is PolicySyntax ? _index.Incoming(owner).SelectMany(reference => _index.Declarations.Where(declaration => declaration.Owner == reference.Reference.Owner)) : [];
        var context = Current(new[] { owner }.Concat(callers).Concat(_index.Outgoing(owner.Owner).SelectMany(_index.Resolve))).ToArray();
        var properties = context.SelectMany(declaration => _properties.GetValueOrDefault(declaration) ?? []).Distinct().ToArray();
        foreach (var group in properties.Where(property => WorkspaceOpaqueText.Named(entry.Node, property.Name) is not null).GroupBy(property => property.Name))
        {
            Add(entry, "opaque", [.. group], group.Key);
        }
    }
}
