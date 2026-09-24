// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Serialization;

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
            SourceComments = [.. parts.SelectMany(part => part.SourceComments).Distinct()],
            Description = FirstDescription(parts.Select(part => (part.Description, part.Location)), $"module '{group.Key}'", context),
            Authorize = CombineAuthorization(parts.Select(part => part.Authorize), $"module '{group.Key}'", context),
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
            Behaviors = InlineBehaviorsOnce(parts.SelectMany(part => part.Behaviors), $"module '{group.Key}'", context),
            UsedBehaviors = UsedBehaviorsOnce(parts.SelectMany(part => part.UsedBehaviors), $"module '{group.Key}'", context),
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
            SourceComments = [.. parts.SelectMany(part => part.SourceComments).Distinct()],
            Description = FirstDescription(parts.Select(part => (part.Description, part.Location)), $"feature '{group.Key}'", context),
            Authorize = CombineAuthorization(parts.Select(part => part.Authorize), $"feature '{group.Key}'", context),
            Contributions = [.. parts.SelectMany(part => part.Contributions ?? [])],
            Behaviors = InlineBehaviorsOnce(parts.SelectMany(part => part.Behaviors), $"feature '{group.Key}'", context),
            UsedBehaviors = UsedBehaviorsOnce(parts.SelectMany(part => part.UsedBehaviors), $"feature '{group.Key}'", context),
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
    /// Combines distinct authorization gates across files without ever weakening an earlier gate.
    /// </summary>
    static AuthorizeSyntax? CombineAuthorization(IEnumerable<AuthorizeSyntax?> declarations, string owner, ParserContext context)
    {
        var kept = new List<AuthorizeSyntax>();
        foreach (var authorization in declarations.OfType<AuthorizeSyntax>())
        {
            var first = kept.Find(earlier =>
                !string.Equals(earlier.Location.Path, authorization.Location.Path, StringComparison.Ordinal) &&
                SyntaxJson.StructurallyEqual(earlier, authorization));
            if (first is not null)
            {
                context.Warning(
                    DiagnosticCodes.DuplicateAuthorizationAcrossFiles,
                    $"An identical authorization is already declared on the {owner} in '{Describe(first.Location.Path)}' - this repeated gate is ignored",
                    authorization.Location);
                continue;
            }

            kept.Add(authorization);
        }

        if (kept.Count == 0)
        {
            return null;
        }

        var requirement = kept[0].Requirement;
        foreach (var next in kept.Skip(1))
        {
            requirement = new LogicalPolicyRequirementSyntax(requirement, LogicalOperator.And, next.Requirement, requirement.Location);
        }

        return new AuthorizeSyntax(requirement, kept[0].Location);
    }

    /// <summary>
    /// Picks the description of a module or feature the files describe between them.
    /// </summary>
    /// <param name="parts">The description each file gives, absent as <c>null</c>, with the location it gives it at.</param>
    /// <param name="owner">The declaration the description belongs to, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The first description given, or <c>null</c> when no file gives one.</returns>
    /// <remarks>
    /// A restated module or feature header does not own a description. A second file describing it claims
    /// the same member even if the text matches, so the folder cannot establish a unique owner.
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

        foreach (var duplicate in described.Skip(1).Where(part =>
            !string.Equals(part.Location.Path, described[0].Location.Path, StringComparison.Ordinal)))
        {
            context.Error(
                DiagnosticCodes.RepeatedDeclarationAcrossFiles,
                $"Duplicate description on {owner} - already declared in '{Describe(described[0].Location.Path)}'",
                duplicate.Location);
        }

        return described[0].Description;
    }

    /// <summary>
    /// Keeps the named behaviors the files attach to one module or feature, ignoring a repeat from another file.
    /// </summary>
    /// <param name="attachments">The <c>uses</c> clauses of every file, in path order.</param>
    /// <param name="owner">The declaration the behaviors are attached to, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The attachments that survive, in path order.</returns>
    /// <remarks>
    /// The same behavior with different arguments is two distinct attachments - a parameterized behavior wired
    /// twice - and both are kept. The same behavior with the same arguments is one attachment written twice.
    /// </remarks>
    static List<UsesBehaviorSyntax> UsedBehaviorsOnce(IEnumerable<UsesBehaviorSyntax> attachments, string owner, ParserContext context) =>
        AttachedOnce(
            attachments,
            (earlier, later) => Signature(earlier) == Signature(later),
            (uses, file) => $"Behavior '{uses.Behavior}' is already attached to the {owner} in '{file}' - this repeated attachment is ignored",
            context);

    /// <summary>
    /// Keeps the inline behaviors the files attach to one module or feature, ignoring a repeat from another file.
    /// </summary>
    /// <param name="attachments">The inline <c>on</c> blocks of every file, in path order.</param>
    /// <param name="owner">The declaration the behaviors are attached to, used in the diagnostic.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The attachments that survive, in path order.</returns>
    /// <remarks>
    /// Identity is structural and ignores where the block is written, so only a block that says exactly the
    /// same thing is a repeat; any difference makes it a distinct attachment that is kept.
    /// </remarks>
    static List<BehaviorSyntax> InlineBehaviorsOnce(IEnumerable<BehaviorSyntax> attachments, string owner, ParserContext context) =>
        AttachedOnce(
            attachments,
            SyntaxJson.StructurallyEqual,
            (_, file) => $"An identical inline behavior is already attached to the {owner} in '{file}' - this repeated attachment is ignored",
            context);

    /// <summary>
    /// Keeps each attachment the first file gives, ignoring an identical one from a later file.
    /// </summary>
    /// <typeparam name="TSyntax">The type of the attachment.</typeparam>
    /// <param name="attachments">The attachments of every file, in path order.</param>
    /// <param name="identical">Whether two attachments are the same attachment.</param>
    /// <param name="message">The diagnostic for a repeat, given the repeat and the file already attaching it.</param>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <returns>The attachments that survive, in path order.</returns>
    /// <remarks>
    /// Attachments are additive, so distinct ones accumulate the same way contributions do. An identical one
    /// from another file would run the behavior once per copy - which is what a folder written before
    /// expansion stopped restating attachments holds, one copy per descendant file - so only the first is
    /// kept and every repeat is reported. It stays a warning because the application is still well defined.
    /// Repeats within one file are the single document's concern and are left as written.
    /// </remarks>
    static List<TSyntax> AttachedOnce<TSyntax>(
        IEnumerable<TSyntax> attachments,
        Func<TSyntax, TSyntax, bool> identical,
        Func<TSyntax, string, string> message,
        ParserContext context)
        where TSyntax : SyntaxNode
    {
        var kept = new List<TSyntax>();
        foreach (var attachment in attachments)
        {
            var first = kept.Find(earlier =>
                !string.Equals(earlier.Location.Path, attachment.Location.Path, StringComparison.Ordinal) && identical(earlier, attachment));
            if (first is not null)
            {
                context.Warning(DiagnosticCodes.DuplicateBehaviorAttachment, message(attachment, Describe(first.Location.Path)), attachment.Location);
                continue;
            }

            kept.Add(attachment);
        }

        return kept;
    }

    static string Signature(UsesBehaviorSyntax uses) =>
        string.Join('\n', uses.Arguments.OrderBy(argument => argument.Name, StringComparer.Ordinal).Select(argument => $"{argument.Name}={argument.Value}").Prepend(uses.Behavior));
}
