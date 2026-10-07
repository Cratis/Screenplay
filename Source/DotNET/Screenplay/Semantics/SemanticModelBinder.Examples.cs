// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static IEnumerable<SemanticSlice> ExampleSlices(IEnumerable<SemanticFeature> features) =>
            features.SelectMany(feature => feature.Slices.Concat(ExampleSlices(feature.Features)));

        void ValidateExampleAdmission(ImmutableArray<SemanticConcept> concepts, ImmutableArray<SemanticCompositeType> types, ImmutableArray<SemanticModule> modules)
        {
            if (expansion.ResolvedExamples.Count == 0) return;

            // Admission is independent of use: an override must not conceal a malformed example value.
            // Validate only stated properties; top-level partial examples are not complete instances.
            var validator = new SemanticValueValidator(concepts.ToDictionary(concept => concept.Id), types.ToDictionary(type => type.Id));
            var commands = modules.SelectMany(module => ExampleSlices(module.Features)).SelectMany(slice => slice.Commands).ToArray();
            foreach (var resolved in expansion.ResolvedExamples)
            {
                var example = resolved.Example;
                foreach (var assignment in example.Values.Concat(example.GeneratedValues))
                {
                    var property = resolved.Properties.Single(item => item.Name == assignment.Property);
                    AdmitExampleValue(example, assignment.Source, property.Type, resolved.Kind, property.IsGenerated, validator);
                }

                if (example.For is not { } destination) continue;
                SemanticTypeReference? destinationType;
                var generatedDestination = false;
                if (resolved.Type is CommandSyntax command)
                {
                    var candidates = commands.Where(item => item.Name == command.Name).ToArray();
                    var bound = candidates.Length == 1 ? candidates[0] : null;
                    var identifier = bound?.Properties.SingleOrDefault(property => property.IsIdentifier);
                    destinationType = bound?.Destination?.Type ?? identifier?.Type;
                    generatedDestination = identifier?.IsGenerated == true;
                }
                else
                {
                    var producerTypes = ProducerEventSourceTypes(((EventSyntax)resolved.Type).Name, []);
                    destinationType = producerTypes.Length == 1 ? producerTypes[0] : null;
                }

                if (destinationType is null or { IsCollection: true } or { IsOptional: true })
                {
                    Error(DiagnosticCodes.UnadmittedSpecificationExampleValue, $"Example '{example.Name}' requires one unambiguous required scalar destination type for 'for'.", destination.Location);
                    continue;
                }

                AdmitExampleValue(example, destination, destinationType, resolved.Kind, validator, generatedDestination);
            }
        }

        void AdmitExampleValue(SpecificationExampleSyntax example, ExpressionSyntax expression, TypeRefSyntax type, string kind, bool generated, SemanticValueValidator validator)
        {
            try
            {
                var target = BindTypeReference(type);
                AdmitExampleValue(example, expression, target, kind, validator, generated);
            }
            catch (InvalidSemanticContract failure)
            {
                Error(DiagnosticCodes.UnadmittedSpecificationExampleValue, $"Example '{example.Name}' value cannot be admitted: {failure.Message}", expression.Location);
            }
        }

        void AdmitExampleValue(SpecificationExampleSyntax example, ExpressionSyntax expression, SemanticTypeReference target, string kind, SemanticValueValidator validator, bool generated = false)
        {
            try
            {
                // Reuse fixture normalization and semantic type validation, including nested null rules,
                // enum members, UUIDs and instants. This does not promote the model's ESM version.
                var value = generated
                    ? BindGeneratedValue(expression, new(default, "example fixture", target, false))
                    : BindConcreteValue(expression, target, $"specification {kind switch { "readmodel" => "read model", _ => kind }}", kind == "readmodel");
                if (value is not null) validator.Validate(value, target, $"example '{example.Name}' value");
            }
            catch (InvalidSemanticContract failure)
            {
                Error(DiagnosticCodes.UnadmittedSpecificationExampleValue, $"Example '{example.Name}' value cannot be admitted: {failure.Message}", expression.Location);
            }
        }
    }
}
