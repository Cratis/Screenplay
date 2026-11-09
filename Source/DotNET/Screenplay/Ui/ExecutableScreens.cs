// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Ui;

/// <summary>
/// Admits a model's screens to its executable semantic model as ESM v42: the stack-neutral data, actions, navigation and
/// layout of every screen, keyed to the identities of the queries, commands and screens they reference.
/// </summary>
/// <remarks>
/// Admission is opt-in: a default compilation keeps its version, bytes and revision and reports screens as deferred
/// (<c>PLAY0269</c>). Scene-specific presentation stays in the <c>screenplay-ui/1</c> contract's <c>scene</c> layer.
/// </remarks>
public static class ExecutableScreens
{
    /// <summary>
    /// Admits the screens of a workspace whose sources compile to an executable model.
    /// </summary>
    /// <param name="workspace">The workspace.</param>
    /// <returns>The admission result.</returns>
    public static ExecutableScreensResult Admit(ScreenplayWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(workspace);
        var compilation = workspace.Compilation.Success ? workspace.Compilation.Value : null;
        if (compilation is null) return new(null, ["The model has no executable model to admit screens to."]);
        return Admit(UiContractBuilder.MergedSyntax(workspace), compilation);
    }

    /// <summary>
    /// Admits the screens of merged application syntax to the executable model it compiled to.
    /// </summary>
    /// <param name="syntax">The merged application syntax.</param>
    /// <param name="compilation">The compilation of <paramref name="syntax"/>.</param>
    /// <param name="utcAssessmentYear">The UTC year whose generation horizon is assessed, or <see langword="null"/> for the current year.</param>
    /// <returns>The admission result: the compilation unchanged when the model has no screens, an ESM v42 compilation
    /// when every screen reference resolves, or the refusals otherwise.</returns>
    public static ExecutableScreensResult Admit(ApplicationSyntax syntax, SemanticCompilation compilation, int? utcAssessmentYear = null)
    {
        ArgumentNullException.ThrowIfNull(syntax);
        ArgumentNullException.ThrowIfNull(compilation);
        var model = compilation.Model;
        if (SemanticModelValidator.UsesScreens(model.Application)) return new(compilation, []);

        var resolver = new ExecutableScreenResolver(syntax, compilation);
        var screens = resolver.Resolve();
        if (!resolver.Refusals.IsEmpty) return new(null, resolver.Refusals);
        if (screens.Count == 0) return new(compilation, []);

        var application = model.Application with { Modules = [.. model.Application.Modules.Select(module => module with { Features = WithScreens(module.Features, screens) })] };
        try
        {
            var admitted = ExecutableSemanticModel.Create(LanguageVersion.V42, SemanticVersion.V42, application, utcAssessmentYear ?? DateTimeOffset.UtcNow.Year);
            return new(SemanticCompilation.Create(admitted, compilation.Documents, compilation.SourceMap).WithSidecarsOf(compilation), []);
        }
        catch (InvalidSemanticContract exception)
        {
            return new(null, [exception.Message]);
        }
    }

    static ImmutableArray<SemanticFeature> WithScreens(ImmutableArray<SemanticFeature> features, IReadOnlyDictionary<SemanticId, ImmutableArray<SemanticScreen>> screens) =>
        [.. features.Select(feature => feature with
        {
            Features = WithScreens(feature.Features, screens),
            Slices = [.. feature.Slices.Select(slice => screens.TryGetValue(slice.Id, out var found) ? slice with { Screens = found } : slice)]
        })];
}
