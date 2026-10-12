// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Workspaces;

internal sealed partial class WorkspacePropertyLinks
{
    static ExpressionSyntax? MappingExpression(MappingSyntax mapping) => mapping switch
    {
        SetMappingSyntax set => set.Source,
        AddMappingSyntax add => add.Value,
        SubtractMappingSyntax subtract => subtract.Value,
        _ => null
    };

    static bool IncompleteProjection(PropertyProjectionScope scope, AuthoredDeclaration[] events, AuthoredDeclaration[] targets, string path) =>
        targets.Length != 1 || events.Length != 1 || scope.Nested is not null || path.Contains('.');

    void ProjectMapping(PropertyProjectionScope scope, AuthoredDeclaration[] events, MappingSyntax mapping, WorkspaceSyntaxEntry feed)
    {
        var use = ProjectionUse(scope, mapping);
        var targets = Members(scope.Models, scope.Nested ?? mapping.Property);
        var expression = MappingExpression(mapping);
        var paths = ProjectionPaths(expression);
        var sources = paths.SelectMany(entry => Members(events, ((PathExpressionSyntax)entry.Node).Path)).Distinct().ToArray();
        var incomplete = IncompleteProjection(scope, events, targets, mapping.Property) ||
            paths.Any(entry => Members(events, ((PathExpressionSyntax)entry.Node).Path).Length != 1) || expression is RawExpressionSyntax;
        _projectionEffects.Add(new(
            feed,
            events,
            targets,
            mapping,
            [.. paths.Select(entry => new WorkspacePropertyProjectionSource(((PathExpressionSyntax)entry.Node).Path, Members(events, ((PathExpressionSyntax)entry.Node).Path)))],
            !incomplete));
        Add(use, "mapping-target", new(mapping.Property, targets, sources is [var only] ? only : null, incomplete));
        foreach (var path in paths)
        {
            var name = ((PathExpressionSyntax)path.Node).Path;
            Add(path, "mapping-source", new(name, Members(events, name), targets is [var target] ? target : null, IncompleteProjection(scope, events, targets, name)));
        }
    }

    WorkspaceSyntaxEntry[] ProjectionPaths(ExpressionSyntax? expression) => expression is null ? [] :
        [.. _entries.Where(entry => entry.Node is PathExpressionSyntax && (ReferenceEquals(entry.Node, expression) || Parents(entry).Any(parent => ReferenceEquals(parent.Node, expression))))];
}
