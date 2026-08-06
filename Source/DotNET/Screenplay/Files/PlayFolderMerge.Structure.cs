// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Combines the module, feature and slice structure the documents of a folder describe between them.
/// </summary>
/// <remarks>
/// Modules and features are the levels a folder structure spreads an application over, so they are the levels
/// that combine by name: every file that names <c>module Invoicing</c> is talking about the same module, and
/// every file that names <c>feature Invoices</c> within it about the same feature. Slices and layouts are
/// leaves - they belong to exactly one file, and a second file claiming one is a duplicate.
/// </remarks>
internal static partial class PlayFolderMerge
{
    static IReadOnlyList<ModuleSyntax> MergeModules(IEnumerable<ModuleSyntax> modules, ParserContext context) =>
        [.. modules.GroupBy(module => module.Name, StringComparer.Ordinal).Select(group => Combine(group, context))];

    static ModuleSyntax Combine(IGrouping<string, ModuleSyntax> group, ParserContext context)
    {
        var parts = group.ToList();
        if (parts.Count == 1)
        {
            return parts[0];
        }

        return parts[0] with
        {
            Description = FirstDescription(parts.Select(part => (part.Description, part.Location)), $"module '{group.Key}'", context),
            Layouts = DeclaredInOneFile(
                parts.SelectMany(part => part.Layouts),
                layout => layout.Name,
                layout => layout.Location,
                "layout",
                context,
                $"module '{group.Key}'"),
            Features = MergeFeatures(parts.SelectMany(part => part.Features), context)
        };
    }

    static IReadOnlyList<FeatureSyntax> MergeFeatures(IEnumerable<FeatureSyntax> features, ParserContext context) =>
        [.. features.GroupBy(feature => feature.Name, StringComparer.Ordinal).Select(group => Combine(group, context))];

    static FeatureSyntax Combine(IGrouping<string, FeatureSyntax> group, ParserContext context)
    {
        var parts = group.ToList();
        if (parts.Count == 1)
        {
            return parts[0];
        }

        return parts[0] with
        {
            Description = FirstDescription(parts.Select(part => (part.Description, part.Location)), $"feature '{group.Key}'", context),
            Features = MergeFeatures(parts.SelectMany(part => part.Features), context),
            Slices = DeclaredInOneFile(
                parts.SelectMany(part => part.Slices),
                slice => slice.Name,
                slice => slice.Location,
                "slice",
                context,
                $"feature '{group.Key}'")
        };
    }

    /// <summary>
    /// Picks the description of a module or feature the files describe between them.
    /// </summary>
    /// <param name="parts">The description each file gives, absent as <c>null</c>, with the location it gives it at.</param>
    /// <param name="owner">The declaration the description belongs to, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The first description given, or <c>null</c> when no file gives one.</returns>
    /// <remarks>
    /// Only the file that owns the module or feature folder is expected to describe it; the files below it
    /// name it without saying anything about it. A second, different description is a genuine disagreement
    /// between two files, so it is reported rather than silently dropped - but it stays a warning, because the
    /// application it describes is still perfectly well defined.
    /// </remarks>
    static string? FirstDescription(
        IEnumerable<(string? Description, SourceLocation Location)> parts,
        string owner,
        ParserContext context)
    {
        var described = parts.Where(part => part.Description is not null).ToList();
        if (described.Count == 0)
        {
            return null;
        }

        foreach (var disagreeing in described.Skip(1)
            .Where(part => !string.Equals(part.Description, described[0].Description, StringComparison.Ordinal)))
        {
            context.Warning(
                $"The {owner} is already described in '{Describe(described[0].Location.Path)}' - keeping that description",
                disagreeing.Location);
        }

        return described[0].Description;
    }
}
