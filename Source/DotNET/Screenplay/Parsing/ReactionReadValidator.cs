// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates what a reaction trigger reads: that each view has an alias when it is read more than once,
/// that the aliases are unique and that the view and the key it is read by exist.
/// </summary>
/// <param name="trigger">The <see cref="ReactionTriggerSyntax"/> to validate.</param>
/// <param name="knownReadModels">The read models the document makes available.</param>
/// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
internal sealed class ReactionReadValidator(ReactionTriggerSyntax trigger, HashSet<string> knownReadModels, ParserContext context)
{
    readonly HashSet<string> _aliases = new(StringComparer.Ordinal);
    HashSet<string> _values = [];

    /// <summary>
    /// Validates every read of the trigger.
    /// </summary>
    public void Validate()
    {
        var reads = (trigger.Reads ?? []).ToList();
        var repeated = reads.GroupBy(read => read.ReadModel, StringComparer.Ordinal)
            .Where(group => group.Count() > 1).Select(group => group.Key).ToHashSet(StringComparer.Ordinal);
        _values = trigger.Data.Select(datum => datum.Name).ToHashSet(StringComparer.Ordinal);
        foreach (var read in reads)
        {
            ReportMissingAlias(read, repeated);
            if (read.Alias is { } alias)
            {
                ReportAliasProblems(read, alias);
            }

            ReportReadModel(read);
            if (read.By is { } by)
            {
                ReportKey(read, by);
            }
        }
    }

    void ReportMissingAlias(ReadsSyntax read, HashSet<string> repeated)
    {
        if (repeated.Contains(read.ReadModel) && read.Alias is null)
        {
            context.Error(
                DiagnosticCodes.MissingReadsAlias,
                $"Reaction trigger reads '{read.ReadModel}' more than once; each read needs an alias",
                read.Location);
        }
    }

    void ReportAliasProblems(ReadsSyntax read, string alias)
    {
        if (!_aliases.Add(alias))
        {
            context.Error(
                DiagnosticCodes.DuplicateReadsAlias,
                $"Reaction trigger already uses reads alias '{alias}'",
                read.Location);
        }

        if (_values.Contains(alias))
        {
            context.Error(
                DiagnosticCodes.ReadsAliasConflictsWithProperty,
                $"Reads alias '{alias}' conflicts with a value of reaction trigger",
                read.Location);
        }
    }

    void ReportReadModel(ReadsSyntax read)
    {
        if (ConceptSyntax.PrimitiveTypes.Contains(read.ReadModel))
        {
            context.Warning(
                DiagnosticCodes.AmbiguousReactionReadsValue,
                $"Reaction trigger reads '{read.ReadModel}' as a view; for a trigger value named 'reads', write '@reads {read.ReadModel}'",
                read.Location);
        }
        else if (!knownReadModels.Contains(read.ReadModel))
        {
            context.Warning(
                DiagnosticCodes.UnknownReadModel,
                $"Unknown read model '{read.ReadModel}' - no projection in the document produces it",
                read.Location);
        }
    }

    void ReportKey(ReadsSyntax read, string by)
    {
        if (trigger.Source is not NamedTriggerSourceSyntax)
        {
            context.Error(
                DiagnosticCodes.ClockTriggerReadsKey,
                $"Clock trigger reads '{read.ReadModel}' by '{by}', but clock triggers take no values",
                read.Location);
        }
        else if (!_values.Contains(by))
        {
            context.Warning(
                DiagnosticCodes.UnknownReactionReadsKey,
                $"Reaction trigger reads '{read.ReadModel}' by '{by}', which is not one of its values",
                read.Location);
        }
    }
}
