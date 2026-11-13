// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Api;
using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Finds the commands, queries and slices a screen names, with their identities and routes when the model has an
/// executable model.
/// </summary>
sealed class UiModelIndex
{
    readonly List<(string Path, SliceSyntax Slice)> _slices = [];
    readonly Dictionary<(string Slice, string Name), ApiOperation> _operations = [];
    readonly Dictionary<string, string> _sliceIds = new(StringComparer.Ordinal);

    /// <summary>
    /// Initializes a new instance of the <see cref="UiModelIndex"/> class.
    /// </summary>
    /// <param name="syntax">The merged application syntax.</param>
    /// <param name="model">The executable model, or null when the model has none.</param>
    /// <param name="extensions">The restored extension contracts, or null when none were resolved.</param>
    internal UiModelIndex(ApplicationSyntax syntax, ExecutableSemanticModel? model, ExtensionResolution? extensions = null)
    {
        Extensions = new(syntax, extensions);
        foreach (var module in syntax.Modules)
        {
            foreach (var feature in module.Features) AddFeature(feature, module.Name);
        }

        if (model is null) return;
        foreach (var operation in ApiDescription.For(model).Operations) _operations[(operation.Slice, operation.Name)] = operation;
        foreach (var (path, slice) in ApiSlices.All(model.Application)) _sliceIds[string.Join('.', path)] = slice.Id.ToString();
    }

    /// <summary>
    /// Gets the components that come from extensions.
    /// </summary>
    internal UiExtensionIndex Extensions { get; }

    /// <summary>
    /// Gets every slice with its dotted path, in declaration order.
    /// </summary>
    internal IReadOnlyList<(string Path, SliceSyntax Slice)> Slices => _slices;

    /// <summary>
    /// Finds the semantic identity of a slice.
    /// </summary>
    /// <param name="slice">The dotted slice path.</param>
    /// <returns>The identity, or null without an executable model.</returns>
    internal string? SliceId(string slice) => _sliceIds.GetValueOrDefault(slice);

    /// <summary>
    /// Finds a command by name, preferring the screen's own slice.
    /// </summary>
    /// <param name="slice">The dotted path of the screen's slice.</param>
    /// <param name="name">The command name.</param>
    /// <returns>The declaring slice path and the command, or null when no slice declares it.</returns>
    internal (string Slice, CommandSyntax Command)? Command(string slice, string name) =>
        Find(slice, candidate => candidate.Commands.FirstOrDefault(command => command.Name == name));

    /// <summary>
    /// Finds a query by name, preferring the screen's own slice.
    /// </summary>
    /// <param name="slice">The dotted path of the screen's slice.</param>
    /// <param name="name">The query name.</param>
    /// <returns>The declaring slice path and the query, or null when no slice declares it.</returns>
    internal (string Slice, QuerySyntax Query)? Query(string slice, string name) =>
        Find(slice, candidate => candidate.Queries.FirstOrDefault(query => query.Name == name));

    /// <summary>
    /// Finds the only command of a slice named by its slice name.
    /// </summary>
    /// <param name="name">The slice name, or its path from any enclosing module or feature.</param>
    /// <returns>The command name, or null when no slice of that name declares exactly one command.</returns>
    internal string? CommandOfSlice(string name) =>
        _slices.Where(entry => entry.Path == name || entry.Path.EndsWith($".{name}", StringComparison.Ordinal))
            .Select(entry => entry.Slice.Commands.ToArray())
            .FirstOrDefault(commands => commands.Length == 1)?[0].Name;

    /// <summary>
    /// Finds the HTTP operation of a command or query.
    /// </summary>
    /// <param name="slice">The dotted path of the declaring slice.</param>
    /// <param name="name">The command or query name.</param>
    /// <returns>The operation, or null without an executable model.</returns>
    internal string? ScreenOfSlice(string name) =>
        _slices.Where(entry => entry.Path == name || entry.Path.EndsWith($".{name}", StringComparison.Ordinal))
            .Select(entry => entry.Slice.Screens.ToArray())
            .FirstOrDefault(screens => screens.Length == 1)?[0].Name;

    internal ApiOperation? Operation(string slice, string name) => _operations.GetValueOrDefault((slice, name));

    (string Slice, T Found)? Find<T>(string slice, Func<SliceSyntax, T?> find)
        where T : class
    {
        foreach (var (path, candidate) in _slices.OrderBy(entry => entry.Path == slice ? 0 : 1))
        {
            if (find(candidate) is { } found) return (path, found);
        }

        return null;
    }

    void AddFeature(FeatureSyntax feature, string path)
    {
        var featurePath = $"{path}.{feature.Name}";
        foreach (var slice in feature.Slices) _slices.Add(($"{featurePath}.{slice.Name}", slice));
        foreach (var child in feature.Features) AddFeature(child, featurePath);
    }
}
