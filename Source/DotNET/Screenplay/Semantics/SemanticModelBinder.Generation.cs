// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Generation;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static bool UsesImplicitUuid(ConceptSyntax concept)
        {
            if (concept.Type != "Uuid") return false;
            if (concept.Generation is not { } generation) return true;

            return generation.Strategy == "uuid" && generation is
            {
                Prefix: null, Case: null, Size: null, Alphabet: null, Count: null,
                Encoding: null, Pattern: null, Checksum: null, Range: null, Rule: null, File: null, Code: null
            };
        }

        SemanticGeneration? BindGeneration(ConceptSyntax concept)
        {
            if (concept.Generation is not { } source) return null;
            if (ImplementationInvariants.GenerationError(source) is { } bodyError)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, bodyError, source.Location);
                return null;
            }
            if (source.Strategy == "sequence")
            {
                Error(DiagnosticCodes.ReservedSequenceGeneration, "Generation strategy 'sequence' is reserved until #619 is admitted.", source.Location);
                return null;
            }

            if (source.Strategy != "rule" && source.Rule is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "A rule name belongs only to rule generation.", source.Location);
                return null;
            }

            if (UsesImplicitUuid(concept)) return null;

            if (concept.Attributes.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Generated concepts with compliance markers are not admitted until #616 phase 4.", source.Location);
                return null;
            }

            var requirement = source.Strategy == "rule" ? BindGenerationImplementation(concept, source) : null;
            if (source.Strategy == "rule" && requirement is null) return null;
            GenerationIntegerRange? range = null;
            try
            {
                if (source.Range is not null) range = GenerationIntegerRange.Parse(source.Range);
                _ = GenerationEntropy.ExactSupportCount(source, utcAssessmentYear);
            }
            catch (Exception error) when (error is GenerationSupportBudgetExceeded or EmptyGenerationSupport or InvalidGenerationPattern or InvalidSemanticContract)
            {
                var code = error switch
                {
                    GenerationSupportBudgetExceeded => DiagnosticCodes.GenerationSupportBudgetExceeded,
                    EmptyGenerationSupport => DiagnosticCodes.EmptyGenerationSupport,
                    _ => DiagnosticCodes.InvalidGenerationArgument
                };
                Error(code, error.Message, source.Location);
                return null;
            }
            var generation = new SemanticGeneration(source.Strategy == "rule" ? "implementation" : source.Strategy)
            {
                Pattern = source.Pattern, Checksum = source.Checksum, Minimum = range?.Minimum, Maximum = range?.Maximum, RequirementId = requirement?.RequirementId,
                Prefix = source.Prefix,
                Case = source.Case,
                Size = source.Strategy == "nanoid" ? source.Size ?? 21 : source.Size,
                Alphabet = source.Alphabet == SemanticGenerationContract.DefaultAlphabet ? null : source.Alphabet,
                Count = source.Count,
                Encoding = source.Encoding
            };
            try
            {
                SemanticGenerationContract.Validate(new(_concepts[concept.Name].Id, concept.Name, Primitive(concept.Type), [.. concept.Values], []) { Generation = generation }, utcAssessmentYear);
            }
            catch (InvalidSemanticContract exception)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, exception.Message, source.Location);
                return null;
            }

            return generation;
        }

        SemanticImplementationRequirement? BindGenerationImplementation(ConceptSyntax concept, GenerationSyntax source)
        {
            if (concept.Type != "String" || concept.Attributes.Any())
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Generation rules require an unmarked String concept; generated compliance markers await #616 phase 4.", source.Location);
                return null;
            }
            if (string.IsNullOrEmpty(source.Rule) || (source.File is null && source.Code is null))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "A named generation rule requires exactly one file or tagged inline body.", source.Location);
                return null;
            }

            return RequireImplementation(SemanticImplementationRole.ValueGeneration, SemanticAddress.ForConcept(_applicationIdentity, concept.Name), source.File, source.Code, source.Rule);
        }

        void ValidateGenerationRule(ConceptSyntax concept, SemanticGeneration? generation, SemanticValidationRule rule, SourceLocation location)
        {
            if (generation is null && (!UsesImplicitUuid(concept) || !AllSlices().SelectMany(entry => entry.Slice.Commands)
                .SelectMany(command => command.Properties).Any(property => property.IsGenerated && string.Equals(property.Type.Name, concept.Name, StringComparison.Ordinal))))
            {
                return;
            }
            var result = RuleConsistency.Check(generation, Primitive(concept.Type), rule, utcAssessmentYear);
            var strategy = generation?.Kind ?? "uuid";
            if (result.Status == RuleConsistencyStatus.Refuted)
            {
                Error(DiagnosticCodes.RefutedGeneratedConceptRule, $"Generation strategy '{strategy}' for concept '{concept.Name}' violates this rule; witness {System.Text.Json.JsonSerializer.Serialize(result.Witness)}.", location);
            }
            else if (result.Status == RuleConsistencyStatus.Unproven)
            {
                Warning(DiagnosticCodes.UnprovenGeneratedConceptRule, $"Generation strategy '{strategy}' for concept '{concept.Name}' has an unproven rule: {result.Reason}.", location);
            }
        }

        void ValidateGenerationEntropy(CommandSyntax command, PropertySyntax property, ConceptSyntax concept)
        {
            var generation = concept.Generation;
            if ((generation?.Strategy == "pattern" || generation?.Strategy == "random") && HasErrors) return;
            var count = GenerationEntropy.ExactSupportCount(generation, utcAssessmentYear);
            if (count is null || GenerationEntropy.HasAtLeastBits(count.Value, 64)) return;
            var bits = GenerationEntropy.SupportBits(count.Value);
            var countAtOnePercent = GenerationEntropy.ValuesAtOnePercentCollision(bits);
            var message = string.Create(CultureInfo.InvariantCulture, $"Generated property '{property.Name}' of '{command.Name}' has {bits:0.##} random bits; 1% collision probability after about {countAtOnePercent:G4} values.");
            if (property.IsIdentifier) Error(DiagnosticCodes.LowGeneratedIdentifierEntropy, message + " Generated identifiers require at least 64 random bits.", property.Location);
            else if (!HasUniqueGenerationTarget(command, property)) Warning(DiagnosticCodes.LowGenerationEntropy, message, property.Location);
        }

        bool HasUniqueGenerationTarget(CommandSyntax command, PropertySyntax property) =>
            AllSlices().SelectMany(entry => entry.Slice.Constraints ?? []).OfType<UniquePropertyConstraintSyntax>()
                .Any(constraint => command.Produces.Any(production => production.Event == constraint.Event &&
                    production.Mappings.Any(mapping => constraint.Property == mapping.Property && !constraint.AdditionalProperties.Any() && mapping.Source is PathExpressionSyntax path && path.Path == property.Name)));
    }
}
