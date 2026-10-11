// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static string? BindingPath(SyntaxNode node) => node switch
    {
        ScreenColumnSyntax column => column.Property,
        ScreenFieldSyntax field => field.Property,
        FormFieldSyntax field => field.Property,
        FormColumnSyntax column => column.Property,
        FormFieldPlacementSyntax placement => placement.Field,
        UiBindingSyntax { BindingKind: UiBindingKind.DataContext or UiBindingKind.QueryResult } binding => binding.Path,
        _ => null
    };

    static bool IncompleteBinding(AuthoredDeclaration[] owners, string path, string? from, AuthoredDeclaration[] sources) =>
        owners.Length != 1 || path.Contains('.') || (from is not null && sources.Length != 1);

    void IndexReaders()
    {
        foreach (var entry in _entries)
        {
            var path = ConditionPath(entry);
            if (path is not null) Add(entry, "condition", new(path, SourceMembers(entry, path), Incomplete: NestedSource(entry, path)));
            IndexTypedReader(entry);
            IndexBinding(entry);
            IndexOpaque(entry);
        }
    }

    string? ConditionPath(WorkspaceSyntaxEntry entry) => entry.Node switch
    {
        ComparisonConditionSyntax condition => condition.Left,
        ValidationRuleSyntax validation => validation.Property,
        PathExpressionSyntax expression when Parents(entry).Any(parent => parent.Node is ConditionSyntax or PolicyConditionSyntax) => expression.Path,
        _ => null
    };

    void IndexBinding(WorkspaceSyntaxEntry entry)
    {
        var path = BindingPath(entry.Node);
        if (string.IsNullOrEmpty(path)) return;
        var ancestors = Parents(entry).ToArray();
        var form = ancestors.Select(parent => parent.Node).OfType<FormSyntax>().FirstOrDefault();
        var owners = form is not null ? Resolve(form.For, ["Command"], entry) : BindingOwners(entry, ancestors);
        var targets = Members(owners, path);
        var from = (entry.Node as FormFieldSyntax)?.From;
        var sources = BindingSources(entry, form, from);
        Add(entry, "screen-binding", new(path, targets, sources is [var source] ? source : null, IncompleteBinding(owners, path, from, sources)));
        if (from is not null)
        {
            Add(entry, "screen-binding", new(from, sources, targets is [var target] ? target : null, targets.Length != 1 || from.Contains('.')));
        }
    }

    AuthoredDeclaration[] BindingSources(WorkspaceSyntaxEntry entry, FormSyntax? form, string? from) =>
        from is not null && form?.Populate is FormPopulateViaQuerySyntax query ? Members(QueryView(query.Query, entry), from) : [];

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
        var callers = owner.Syntax is PolicySyntax ? PolicyCallers(owner) : [];
        var context = Current(new[] { owner }.Concat(callers).Concat(_index.Outgoing(owner.Owner).SelectMany(_index.Resolve))).ToArray();
        var properties = context.SelectMany(declaration => _properties.GetValueOrDefault(declaration) ?? []).Distinct().ToArray();
        foreach (var group in properties.Where(property => WorkspaceOpaqueText.Named(entry.Node, property.Name) is not null).GroupBy(property => property.Name))
        {
            Add(entry, "opaque", new(group.Key, [.. group]));
        }
    }
}
