// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Completeness;

static class DataBindingCompleteness
{
    internal static IEnumerable<Diagnostic> Check(ConsistencyDeclarations declarations) => declarations.Slices.SelectMany(entry =>
        entry.Slice.Screens.SelectMany(screen => CheckContainer(screen.Directives, [], entry.Scope, declarations)));

    static IEnumerable<Diagnostic> CheckContainer(
        IEnumerable<ScreenDirectiveSyntax> directives,
        IReadOnlyList<Binding> ancestors,
        DeclarationScope scope,
        ConsistencyDeclarations declarations)
    {
        var container = directives.ToArray();
        var visible = ancestors.ToList();
        foreach (var data in container.OfType<ScreenDataSyntax>())
        {
            var query = declarations.Resolve(data.Query, scope, slice => slice.Queries, query => query.Name);
            if (query is null)
            {
                continue;
            }

            var binding = new Binding(data, query.Value.Node);
            if (visible.Exists(previous => previous.Data.Type.Name == data.Type.Name &&
                (previous.Data.Type.IsCollection != data.Type.IsCollection || !ReferenceEquals(previous.Query, binding.Query) || previous.Data.By != data.By)))
            {
                yield return Diagnostic.Warning(
                    DiagnosticCodes.ConflictingScreenDataBinding,
                    $"Data binding '{data.Type.Name}' conflicts with another visible binding - use the same cardinality, query and by parameter, or separate containers",
                    data.Location);
            }
            visible.Add(binding);

            var boundView = declarations.View(data.Type.Name, scope);
            var returnedView = declarations.View(query.Value.Node.ReturnType.Name, query.Value.Scope);
            if (boundView is not null && returnedView is not null &&
                (boundView.Name != returnedView.Name || !boundView.Scope.Segments.SequenceEqual(returnedView.Scope.Segments, StringComparer.Ordinal) ||
                    data.Type.IsCollection != query.Value.Node.ReturnType.IsCollection))
            {
                yield return Diagnostic.Warning(
                    DiagnosticCodes.ScreenDataQueryMismatch,
                    $"Data binding '{data.Type.Name}' does not match query '{data.Query}' return type or cardinality",
                    data.Location);
            }
        }

        foreach (var child in container.SelectMany(ScreenContainers.Children))
        {
            foreach (var diagnostic in CheckContainer(child, visible, scope, declarations))
            {
                yield return diagnostic;
            }
        }
    }

    sealed record Binding(ScreenDataSyntax Data, QuerySyntax Query);
}
