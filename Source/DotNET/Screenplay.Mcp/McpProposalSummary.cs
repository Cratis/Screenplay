// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Tells a reader in one paragraph what a proposal changes and whether it is ready to apply.
/// </summary>
static class McpProposalSummary
{
    const int MaximumListedDeclarations = 12;
    const int MaximumListedCodes = 6;

    /// <summary>
    /// Counts proposal-time authoring diagnostics by severity.
    /// </summary>
    /// <param name="diagnostics">The authoring diagnostics of the proposal.</param>
    /// <returns>The counts and distinct codes.</returns>
    internal static object Severities(ImmutableArray<Diagnostic> diagnostics) => new
    {
        errors = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error),
        warnings = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning),
        information = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information),
        codes = diagnostics.Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray()
    };

    /// <summary>
    /// Builds the human summary of a stored proposal.
    /// </summary>
    /// <param name="proposalId">The proposal identifier.</param>
    /// <param name="proposal">The proposal.</param>
    /// <param name="droppedComments">How many comments the proposal drops.</param>
    /// <param name="introducedExecutableErrors">How many executable errors the proposal introduces.</param>
    /// <returns>The summary text.</returns>
    internal static string Text(string proposalId, IMcpProposal proposal, int droppedComments, int introducedExecutableErrors)
    {
        var after = proposal.Workspace;
        var authoringDiagnostics = AuthoringDiagnostics(proposal);
        var (added, removed) = McpVisualization.DrawnChanges(proposal.Before, after);
        var text = new StringBuilder($"Proposal {proposalId} changes {Documents(proposal.WritePlan.Entries)}.");
        text.Append(Declarations(" Adds", added)).Append(Declarations(" Removes", removed));
        if (added.Length == 0 && removed.Length == 0)
        {
            text.Append(" It adds or removes no module, feature, slice, command, event, read model, screen or reaction.");
        }

        text.Append(' ').Append(Diagnostics(authoringDiagnostics));
        if (droppedComments > 0)
        {
            text.Append($" It drops {droppedComments} comment(s); inspect read-proposal view=dropped-comments.");
        }

        text.Append(' ').Append(Readiness(after, introducedExecutableErrors));
        return text.Append(" Review it with read-proposal, then apply it with this proposalId.").ToString();
    }

    /// <summary>
    /// The diagnostics reported while proposing; proposals that do not author source report none.
    /// </summary>
    /// <param name="proposal">The proposal.</param>
    /// <returns>The authoring diagnostics.</returns>
    internal static ImmutableArray<Diagnostic> AuthoringDiagnostics(IMcpProposal proposal) =>
        proposal is McpAuthoringProposal authored ? authored.Result.AuthoringDiagnostics : [];

    static string Documents(ImmutableArray<WorkspaceWriteEntry> entries)
    {
        var kinds = entries.GroupBy(entry => entry.Kind).OrderBy(group => group.Key)
            .Select(group => $"{group.Count()} {group.Key.ToString().ToLowerInvariant()}");
        return entries.IsEmpty ? "no documents" : $"{entries.Length} document(s): {string.Join(", ", kinds)}";
    }

    static string Declarations(string verb, string[] declarations)
    {
        if (declarations.Length == 0)
        {
            return string.Empty;
        }

        var listed = string.Join(", ", declarations.Take(MaximumListedDeclarations));
        var more = declarations.Length > MaximumListedDeclarations ? $" and {declarations.Length - MaximumListedDeclarations} more" : string.Empty;
        return $"{verb} {listed}{more}.";
    }

    static string Diagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        if (diagnostics.IsEmpty)
        {
            return "Authoring checks reported nothing.";
        }

        var errors = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var warnings = diagnostics.Count(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning);
        var information = diagnostics.Length - errors - warnings;
        var codes = diagnostics.Select(diagnostic => diagnostic.Code).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToArray();
        var listed = string.Join(", ", codes.Take(MaximumListedCodes)) + (codes.Length > MaximumListedCodes ? ", ..." : string.Empty);
        var canonicalized = codes.Contains(DiagnosticCodes.AuthoringSourceNormalization, StringComparer.Ordinal)
            ? $" {DiagnosticCodes.AuthoringSourceNormalization} means a touched document is printed canonically."
            : string.Empty;
        return $"Authoring checks reported {errors} error(s), {warnings} warning(s) and {information} information ({listed}); read-proposal view=diagnostics lists them.{canonicalized}";
    }

    static string Readiness(ScreenplayWorkspace after, int introducedExecutableErrors)
    {
        if (introducedExecutableErrors > 0)
        {
            return $"The source is accepted but introduces {introducedExecutableErrors} executable error(s); see executableGuidance.";
        }

        return after.Compilation.Success
            ? "The result is executable."
            : "The source is accepted; the model was already outside the executable subset and this proposal adds no new executable errors.";
    }
}
