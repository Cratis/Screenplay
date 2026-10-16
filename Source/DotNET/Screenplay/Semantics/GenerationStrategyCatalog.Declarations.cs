// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public static partial class GenerationStrategyCatalog
{
    internal static ImmutableArray<SemanticConceptFinding> CheckDeclaration(string primitive, GenerationSyntax generation)
    {
        var findings = ImmutableArray.CreateBuilder<SemanticConceptFinding>();
        void Error(string code, string message, string? option) => findings.Add(new(code, DiagnosticSeverity.Error, message, option is null ? SemanticConceptFindingTarget.Generation : SemanticConceptFindingTarget.Option, option));
        if (Find(generation.Strategy) is not { } descriptor)
        {
            Error(DiagnosticCodes.UnknownGenerationStrategy, $"Unknown generation strategy '{generation.Strategy}'.", null);
            return findings.ToImmutable();
        }

        var supplied = Values(generation).ToDictionary(value => value.Name, value => value.Value, StringComparer.Ordinal);
        foreach (var option in descriptor.Options.Where(option => option.Required && option.Placement == GenerationOptionPlacement.Argument && supplied[option.Name] is null))
        {
            Error(DiagnosticCodes.InvalidGenerationArgument, $"Missing or invalid argument for generation strategy '{generation.Strategy}'.", option.Name);
        }
        foreach (var (name, value) in supplied.Where(value => value.Value is not null))
        {
            var option = descriptor.Options.FirstOrDefault(option => option.Name == name);
            if (option is null || (option.Placement == GenerationOptionPlacement.Modifier && !AllowsModifier(generation.Strategy, primitive, generation.Encoding, name)))
            {
                Error(DiagnosticCodes.InvalidGenerationModifier, $"Generation modifier '{name}' is not allowed for '{generation.Strategy}' on '{primitive}'.", name);
                continue;
            }
            if ((option.Kind == GenerationOptionKind.Choice && !option.Choices.Contains((string)value!, StringComparer.Ordinal)) ||
                (option.Kind == GenerationOptionKind.Name && !ValidText(option, (string)value!)))
            {
                var message = option.Placement == GenerationOptionPlacement.Argument ? $"Missing or invalid argument for generation strategy '{generation.Strategy}'." : $"Missing or invalid value for generation modifier '{name}'.";
                Error(DiagnosticCodes.InvalidGenerationArgument, message, name);
            }
        }
        if (generation.Range is { } range)
        {
            try
            {
                _ = GenerationIntegerRange.Parse(range);
            }
            catch (InvalidSemanticContract)
            {
                Error(DiagnosticCodes.InvalidGenerationArgument, $"Missing or invalid argument for generation strategy '{generation.Strategy}'.", "range");
            }
        }

        // Keep independent value findings, but never assess a pattern with invalid choices or arguments.
        var assessable = findings.Count == 0 ? generation : generation with { Pattern = null };
        ValidateValues(primitive, assessable, (diagnostic, option) => findings.Add(new(diagnostic.Code, diagnostic.Severity, diagnostic.Message, option is null ? SemanticConceptFindingTarget.Generation : SemanticConceptFindingTarget.Option, option)));

        return findings.ToImmutable();
    }

    static IEnumerable<(string Name, object? Value)> Values(GenerationSyntax generation) =>
    [
        ("prefix", generation.Prefix), ("case", generation.Case), ("size", generation.Size), ("alphabet", generation.Alphabet),
        ("count", generation.Count), ("encoding", generation.Encoding), ("pattern", generation.Pattern), ("checksum", generation.Checksum),
        ("range", generation.Range), ("rule", generation.Rule)
    ];
}
