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
/// every file that names <c>feature Invoices</c> within it about the same feature. Slices and templates are
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
            ScreenTemplates = DeclaredInOneFile(
                parts.SelectMany(part => part.ScreenTemplates),
                template => template.Name,
                template => template.Location,
                "screen template",
                context,
                $"module '{group.Key}'"),
            DialogTemplates = DeclaredInOneFile(
                parts.SelectMany(part => part.DialogTemplates ?? []),
                template => template.Name,
                template => template.Location,
                "dialog template",
                context,
                $"module '{group.Key}'"),
            Forms = DeclaredInOneFile(
                parts.SelectMany(part => part.Forms ?? []),
                form => form.Name,
                form => form.Location,
                "form",
                context,
                $"module '{group.Key}'"),
            Contributions = [.. parts.SelectMany(part => part.Contributions ?? [])],
            Behaviors = [.. parts.SelectMany(part => part.Behaviors)],
            UsedBehaviors = AttachedAcrossFiles(parts.SelectMany(part => part.UsedBehaviors), $"module '{group.Key}'", context),
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
            Contributions = [.. parts.SelectMany(part => part.Contributions ?? [])],
            Behaviors = [.. parts.SelectMany(part => part.Behaviors)],
            UsedBehaviors = AttachedAcrossFiles(parts.SelectMany(part => part.UsedBehaviors), $"feature '{group.Key}'", context),
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
                DiagnosticCodes.ConflictingDescriptionAcrossFiles,
                $"The {owner} is already described in '{Describe(described[0].Location.Path)}' - keeping that description",
                disagreeing.Location);
        }

        return described[0].Description;
    }

    /// <summary>
    /// Keeps every named behavior the files attach to one module or feature, reporting a repeated attachment.
    /// </summary>
    /// <param name="attachments">The <c>uses</c> clauses of every file, in path order.</param>
    /// <param name="owner">The declaration the behaviors are attached to, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>Every attachment, in path order.</returns>
    /// <remarks>
    /// Attachments are additive, so every one is kept - the same as contributions. The same behavior attached
    /// again with the same arguments from another file runs twice and almost certainly repeats a line by
    /// mistake, so it is reported; it stays a warning because the application is still well defined. The same
    /// behavior with different arguments is two distinct attachments - a parameterized behavior wired twice -
    /// and is not reported.
    /// </remarks>
    static List<UsesBehaviorSyntax> AttachedAcrossFiles(
        IEnumerable<UsesBehaviorSyntax> attachments,
        string owner,
        ParserContext context)
    {
        var kept = attachments.ToList();
        var first = new Dictionary<string, SourceLocation>(StringComparer.Ordinal);
        foreach (var uses in kept)
        {
            var signature = string.Join('\n', uses.Arguments.OrderBy(argument => argument.Name, StringComparer.Ordinal).Select(argument => $"{argument.Name}={argument.Value}").Prepend(uses.Behavior));
            if (!first.TryAdd(signature, uses.Location) && !string.Equals(first[signature].Path, uses.Location.Path, StringComparison.Ordinal))
            {
                context.Warning(
                    DiagnosticCodes.DuplicateBehaviorAttachment,
                    $"Behavior '{uses.Behavior}' is already attached to the {owner} in '{Describe(first[signature].Path)}' - both attachments run",
                    uses.Location);
            }
        }

        return kept;
    }
}
