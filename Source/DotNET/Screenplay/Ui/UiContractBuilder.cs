// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
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
        return For(MergedSyntax(workspace), model, workspace.ApplicationName);
    }

    /// <summary>
    /// Derives the UI contract of an application.
    /// </summary>
    /// <param name="syntax">The merged application syntax.</param>
    /// <param name="model">The executable model of the same sources, or null when there is none.</param>
    /// <param name="application">The application name.</param>
    /// <returns>The UI contract.</returns>
    public static UiContract For(ApplicationSyntax syntax, ExecutableSemanticModel? model, string application)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        var index = new UiModelIndex(syntax, model);
        var screens = index.Slices
            .SelectMany(entry => entry.Slice.Screens.Select(screen => new UiScreenReader(index, entry.Path).Read(screen)))
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

    internal static ApplicationSyntax MergedSyntax(ScreenplayWorkspace workspace)
    {
        var roots = WorkspaceSyntaxIndex.Create(workspace).Entries
            .Where(entry => entry.Parent is null && entry.Node is ApplicationSyntax)
            .Select(entry => new CompilationResult<ApplicationSyntax>((ApplicationSyntax)entry.Node, []))
            .ToArray();
        return PlayFolderMerge.Merge(roots).Value ?? throw new InvalidOperationException("The workspace sources do not merge into one application.");
    }
}
