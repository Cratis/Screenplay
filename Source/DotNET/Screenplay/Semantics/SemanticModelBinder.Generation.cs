// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        SemanticGeneration? BindGeneration(ConceptSyntax concept)
        {
            if (concept.Generation is not { } source) return null;
            if (source.Strategy == "sequence")
            {
                Error(DiagnosticCodes.ReservedSequenceGeneration, "Generation strategy 'sequence' is reserved until #619 is admitted.", source.Location);
                return null;
            }

            if (source.Strategy == "pattern" || source.Strategy == "random" || source.Strategy == "rule")
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Generation strategy '{source.Strategy}' is parsed but not executable until #616 phase 3.", source.Location);
                return null;
            }

            if (source.Pattern is not null || source.Checksum is not null || source.Range is not null || source.Rule is not null)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Preview generation arguments cannot be attached to admitted strategies (#616).", source.Location);
                return null;
            }

            if (concept.Type == "Uuid" && source.Strategy == "uuid" && source is { Prefix: null, Case: null, Size: null, Alphabet: null, Count: null, Encoding: null }) return null;

            if (concept.Attributes.Any() || (concept.Validations ?? []).Any(validation => validation is CodeValidateSyntax ||
                (validation is DeclarativeValidateSyntax declarative && (declarative.Rules.Any() || (declarative.Requirements ?? []).Any()))))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, "Generated concepts with compliance markers or validation rules are not admitted in #616 phase 2b-1.", source.Location);
                return null;
            }

            var generation = new SemanticGeneration(source.Strategy)
            {
                Prefix = source.Prefix,
                Case = source.Case,
                Size = source.Strategy == "nanoid" ? source.Size ?? 21 : source.Size,
                Alphabet = source.Alphabet == SemanticGenerationContract.DefaultAlphabet ? null : source.Alphabet,
                Count = source.Count,
                Encoding = source.Encoding
            };
            try
            {
                SemanticGenerationContract.Validate(new(_concepts[concept.Name].Id, concept.Name, Primitive(concept.Type), [.. concept.Values], []) { Generation = generation });
            }
            catch (InvalidSemanticContract exception)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, exception.Message, source.Location);
                return null;
            }

            return generation;
        }

        void ValidateGenerationEntropy(CommandSyntax command, PropertySyntax property, ConceptSyntax concept)
        {
            var generation = concept.Generation;
            var bits = generation?.Strategy switch
            {
                null or "uuid" => 122,
                "uuidv7" or "typeid" => 74,
                "ulid" => 80,
                "nanoid" => (generation.Size ?? 21) * Math.Log2((generation.Alphabet ?? SemanticGenerationContract.DefaultAlphabet).Length),
                "bytes" when generation.Count is { } count => 8 * count,
                _ => 0
            };
            if (bits >= 64 || generation?.Strategy == "pattern" || generation?.Strategy == "random" || generation?.Strategy == "rule" || generation?.Strategy == "sequence") return;
            var countAtOnePercent = Math.Sqrt(2 * Math.Pow(2, bits) * Math.Log(1 / 0.99));
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
