// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Extensions;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Derives the UI contract of a model.
/// </summary>
public static class UiContractBuilder
{
    /// <summary>
    /// Derives the UI contract of a workspace. Identities and routes are included when the workspace has an executable model.
    /// </summary>
    /// <param name="workspace">The workspace.</param>
    /// <returns>The UI contract.</returns>
    public static UiContract For(ScreenplayWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var model = workspace.Compilation.Success ? workspace.Compilation.Value?.Model : null;
        var syntax = MergedSyntax(workspace);
        var documents = workspace.Compilation.Success ? workspace.Compilation.Value!.Documents.Documents.Select(document => document.DisplayPath) : [];
        return For(syntax, model, workspace.ApplicationName, ExtensionContractResolver.Current.Resolve(syntax, documents));
    }

    /// <summary>
    /// Derives the UI contract of an application.
    /// </summary>
    /// <param name="syntax">The merged application syntax.</param>
    /// <param name="model">The executable model of the same sources, or null when there is none.</param>
    /// <param name="application">The application name.</param>
    /// <param name="extensions">The restored extension contracts, or null when none were resolved.</param>
    /// <returns>The UI contract.</returns>
    public static UiContract For(ApplicationSyntax syntax, ExecutableSemanticModel? model, string application, ExtensionResolution? extensions = null)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        var index = new UiModelIndex(syntax, model, extensions);
        var screens = index.Slices
            .SelectMany(entry => entry.Slice.Screens.Select(screen => new UiScreenReader(index, entry.Path).Read(screen)))
            .Concat(Adopted(syntax, index))
            .ToArray();
        return new(UiContract.Version, model?.Application.Name ?? application, screens) { ModelRevision = model?.Revision.ToString() };
    }

    /// <summary>
    /// Reads the UI contract from the screens an ESM v42 executable model holds, with no sources and no Scene layer.
    /// </summary>
    /// <param name="model">An executable model whose screens were admitted with <see cref="ExecutableScreens"/>.</param>
    /// <returns>The contract of every screen the model holds.</returns>
    public static UiContract For(ExecutableSemanticModel model)
    {
        ArgumentNullException.ThrowIfNull(model);
        return new UiExecutableReader(model).Read();
    }

    /// <summary>
    /// Keeps the screens of one module, feature or slice.
    /// </summary>
    /// <param name="contract">The contract.</param>
    /// <param name="scope">The scope as <c>Module[.Feature...][.Slice]</c>.</param>
    /// <returns>The contract with only the screens in scope.</returns>
    public static UiContract Select(UiContract contract, string scope)
    {
        ArgumentNullException.ThrowIfNull(contract);
        var screens = contract.Screens.Where(screen => screen.Slice == scope || screen.Slice.StartsWith($"{scope}.", StringComparison.Ordinal)).ToArray();
        if (screens.Length == 0) throw new InvalidSemanticModelScope($"Scope '{scope}' names no module, feature or slice with a screen.");
        return contract with { Screens = screens };
    }

    // A free screen enters the contract once per slice that adopts it: the adopting slice, and the data its provisions resolve.
    // A free screen no slice adopts is design-only and has no slice to serve it, so it is not part of the contract.
    internal static IEnumerable<UiScreen> Adopted(ApplicationSyntax syntax, UiModelIndex index)
    {
        var scoped = ScreenplayValidator.ScopedScreens(syntax).ToList();
        var pool = scoped.ConvertAll(entry => new Declaration(entry.Screen.Name, entry.Scope));
        var scopes = new Dictionary<SliceSyntax, DeclarationScope>(ReferenceEqualityComparer.Instance);
        foreach (var (scopedSlice, sliceScope) in ScreenplayValidator.ScopedSlices(syntax)) scopes[scopedSlice] = sliceScope;
        foreach (var (path, slice) in index.Slices)
        {
            foreach (var used in slice.UsedScreens)
            {
                if (!scopes.TryGetValue(slice, out var scope) || ReferenceResolver.Resolve(used.Screen, scope, pool).Resolved is not { } resolved)
                {
                    continue;
                }

                var free = scoped[pool.IndexOf(resolved)];
                yield return new UiScreenReader(index, path, used, string.Join('.', free.Scope.Segments)).Read(free.Screen);
            }
        }
    }

    internal static ApplicationSyntax MergedSyntax(ScreenplayWorkspace workspace)
    {
        var roots = WorkspaceSyntaxIndex.Create(workspace).Entries
            .Where(entry => entry.Parent is null && entry.Node is ApplicationSyntax)
            .Select(entry => new CompilationResult<ApplicationSyntax>((ApplicationSyntax)entry.Node, []))
            .ToArray();
        return PlayFolderMerge.Merge(roots).Value ?? throw new InvalidOperationException("The workspace sources do not merge into one application.");
    }
}
