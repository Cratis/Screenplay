// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Validates what a command reads, and the operands of the requirements that refer to it.
/// </summary>
/// <param name="command">The <see cref="CommandSyntax"/> to validate.</param>
/// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
internal sealed class CommandReadValidator(CommandSyntax command, ParserContext context)
{
    readonly List<ReadsSyntax> _declarations = [.. command.Reads ?? []];

    /// <summary>
    /// Validates that what the command reads exists, and that the key it reads by is one of its own properties.
    /// </summary>
    /// <param name="knownReadModels">The read models the document's projections produce.</param>
    public void ValidateReads(HashSet<string> knownReadModels)
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet(StringComparer.Ordinal);
        var repeatedViews = _declarations.GroupBy(read => read.ReadModel, StringComparer.Ordinal)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToHashSet(StringComparer.Ordinal);
        var aliases = new HashSet<string>(StringComparer.Ordinal);

        foreach (var reads in _declarations)
        {
            ReportMissingAlias(reads, repeatedViews);
            if (reads.Alias is { } alias)
            {
                ReportAliasProblems(reads, alias, aliases, properties);
            }

            ReportUnknownReadModel(reads, knownReadModels);
            ReportUnknownKey(reads, properties);
        }
    }

    /// <summary>
    /// Validates that the operands of the command's requirements name something the command can see.
    /// </summary>
    /// <remarks>
    /// A requirement is only worth stating if a consumer can tell what it is about. An operand is either a
    /// property of the command or a path into state the command declares it reads - anything else names
    /// something no reader of the document can resolve.
    /// </remarks>
    public void ValidateRequirements()
    {
        var properties = command.Properties.Select(property => property.Name).ToHashSet();
        var reads = _declarations.Where(read => _declarations.Count(other => other.ReadModel == read.ReadModel) == 1)
            .Select(read => read.ReadModel)
            .Concat(_declarations.Select(read => read.Alias).OfType<string>())
            .ToHashSet(StringComparer.Ordinal);

        foreach (var requirement in command.Validations.OfType<DeclarativeValidateSyntax>()
            .SelectMany(validate => validate.Requirements ?? []))
        {
            foreach (var operand in Operands(requirement.Condition))
            {
                ValidateOperand(operand, requirement.Location, properties, reads);
            }
        }
    }

    /// <summary>
    /// Yields the left hand operand of every comparison in a condition.
    /// </summary>
    /// <param name="condition">The <see cref="ConditionSyntax"/> to walk.</param>
    /// <returns>The operands, in the order they appear.</returns>
    static IEnumerable<string> Operands(ConditionSyntax condition) => condition switch
    {
        ComparisonConditionSyntax comparison => [comparison.Left],
        LogicalConditionSyntax logical => Operands(logical.Left).Concat(Operands(logical.Right)),
        _ => []
    };

    void ReportMissingAlias(ReadsSyntax reads, HashSet<string> repeatedViews)
    {
        if (repeatedViews.Contains(reads.ReadModel) && reads.Alias is null)
        {
            context.Error(
                DiagnosticCodes.MissingReadsAlias,
                $"Command '{command.Name}' reads '{reads.ReadModel}' more than once; each read needs an alias",
                reads.Location);
        }
    }

    void ReportAliasProblems(ReadsSyntax reads, string alias, HashSet<string> aliases, HashSet<string> properties)
    {
        if (!aliases.Add(alias))
        {
            context.Error(
                DiagnosticCodes.DuplicateReadsAlias,
                $"Command '{command.Name}' already uses reads alias '{alias}'",
                reads.Location);
        }

        if (properties.Contains(alias))
        {
            context.Error(
                DiagnosticCodes.ReadsAliasConflictsWithProperty,
                $"Reads alias '{alias}' conflicts with a property of command '{command.Name}'",
                reads.Location);
        }
    }

    void ReportUnknownReadModel(ReadsSyntax reads, HashSet<string> knownReadModels)
    {
        if (!knownReadModels.Contains(reads.ReadModel))
        {
            context.Warning(
                DiagnosticCodes.UnknownReadModel,
                $"Unknown read model '{reads.ReadModel}' - no projection in the document produces it",
                reads.Location);
        }
    }

    void ReportUnknownKey(ReadsSyntax reads, HashSet<string> properties)
    {
        if (reads.By is { } by && !properties.Contains(by))
        {
            context.Warning(
                DiagnosticCodes.UnknownReadsKey,
                $"Command '{command.Name}' reads '{reads.ReadModel}' by '{by}', which is not one of its properties",
                reads.Location);
        }
    }

    void ValidateOperand(string operand, SourceLocation location, HashSet<string> properties, HashSet<string> reads)
    {
        var separator = operand.IndexOf('.', StringComparison.Ordinal);
        if (separator < 0)
        {
            if (!properties.Contains(operand))
            {
                context.Warning(
                    DiagnosticCodes.UnknownRequirementOperand,
                    $"Command '{command.Name}' requires '{operand}', which is neither one of its properties nor state it reads",
                    location);
            }

            return;
        }

        var source = operand[..separator];
        if (!reads.Contains(source))
        {
            ReportUnknownOperandSource(operand, source, location);
        }
    }

    void ReportUnknownOperandSource(string operand, string source, SourceLocation location)
    {
        var aliases = _declarations.Where(read => read.ReadModel == source).Select(read => read.Alias).OfType<string>().ToList();
        var repeated = _declarations.Count(read => read.ReadModel == source) > 1;
        context.Warning(
            DiagnosticCodes.UnknownRequirementOperandSource,
            repeated
                ? $"Command '{command.Name}' requires '{operand}', but reads '{source}' more than once; qualify the path with one of its aliases ({string.Join(", ", aliases)})."
                : $"Command '{command.Name}' requires '{operand}', but does not declare 'reads {source}'",
            location);
    }
}
