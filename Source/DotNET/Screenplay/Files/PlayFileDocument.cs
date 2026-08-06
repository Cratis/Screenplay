// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Builds the document each file of an expanded folder structure holds.
/// </summary>
/// <remarks>
/// Each file carries exactly one thing plus the scaffolding needed to place it: a module file carries the
/// module's own description and layouts, a feature file its description, a slice file the slice. Everything
/// above it is restated stripped of its own content, so nothing is written twice and merging on the way back
/// in has nothing to disagree about.
/// </remarks>
internal static class PlayFileDocument
{
    /// <summary>
    /// Builds the document of a module file - the module itself, without its features.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> the file belongs to.</param>
    /// <returns>The <see cref="ApplicationSyntax"/> to print.</returns>
    public static ApplicationSyntax ForModule(ModuleSyntax module) => Document(module with { Features = [] });

    /// <summary>
    /// Builds the document of a feature file - the feature itself, without its sub features or slices.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> the feature belongs to.</param>
    /// <param name="ancestors">The <see cref="FeatureSyntax">features</see> the feature is nested in, outermost first.</param>
    /// <param name="feature">The <see cref="FeatureSyntax"/> the file belongs to.</param>
    /// <returns>The <see cref="ApplicationSyntax"/> to print.</returns>
    public static ApplicationSyntax ForFeature(ModuleSyntax module, IReadOnlyList<FeatureSyntax> ancestors, FeatureSyntax feature) =>
        Document(Bare(module) with { Features = [Nest(ancestors, feature with { Features = [], Slices = [] })] });

    /// <summary>
    /// Builds the document of a slice file - the slice, under the module and features it belongs to.
    /// </summary>
    /// <param name="module">The <see cref="ModuleSyntax"/> the slice belongs to.</param>
    /// <param name="ancestors">The <see cref="FeatureSyntax">features</see> the owning feature is nested in, outermost first.</param>
    /// <param name="feature">The <see cref="FeatureSyntax"/> the slice belongs to.</param>
    /// <param name="slice">The <see cref="SliceSyntax"/> the file holds.</param>
    /// <returns>The <see cref="ApplicationSyntax"/> to print.</returns>
    public static ApplicationSyntax ForSlice(
        ModuleSyntax module,
        IReadOnlyList<FeatureSyntax> ancestors,
        FeatureSyntax feature,
        SliceSyntax slice) =>
        Document(Bare(module) with { Features = [Nest(ancestors, Bare(feature) with { Slices = [slice] })] });

    static ApplicationSyntax Document(ModuleSyntax module) => new([], [], [], [module], SourceLocation.Start);

    static FeatureSyntax Nest(IReadOnlyList<FeatureSyntax> ancestors, FeatureSyntax innermost)
    {
        var current = innermost;
        for (var level = ancestors.Count - 1; level >= 0; level--)
        {
            current = Bare(ancestors[level]) with { Features = [current] };
        }

        return current;
    }

    static ModuleSyntax Bare(ModuleSyntax module) => module with { Layouts = [], Features = [], Description = null };

    static FeatureSyntax Bare(FeatureSyntax feature) => feature with { Features = [], Slices = [], Description = null };
}
