// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        readonly Dictionary<CommandSyntax, SemanticCommand> _exampleCommands = new(ReferenceEqualityComparer.Instance);

        void ValidateExampleAdmission(SemanticApplication application)
        {
            if (expansion.ResolvedExamples.Count == 0 && !expansion.Specifications.Any(specification => specification.Case is not null)) return;

            // Admission is independent of use: an override must not conceal a malformed example value.
            // Validate only stated properties; top-level partial examples are not complete instances.
            var validator = new SemanticValueValidator(application.Concepts.ToDictionary(concept => concept.Id), application.Types.ToDictionary(type => type.Id));
            foreach (var table in expansion.Specifications.Where(specification => specification.Case is not null).Select(specification => specification.Authored).Distinct(ReferenceEqualityComparer.Instance).OfType<SpecificationSyntax>())
            {
                foreach (var row in table.Cases)
                {
                    foreach (var assignment in row.Values)
                    {
                        var parameter = table.Parameters.Single(item => item.Name == assignment.Property);
                        try
                        {
                            var target = BindTypeReference(parameter.Type);
                            var value = BindConcreteValue(assignment.Source, target, "specification case", true);
                            if (value is not null) validator.Validate(value, target, $"case '{row.Name}' parameter '{parameter.Name}'");
                        }
                        catch (InvalidSemanticContract failure)
                        {
                            Error(DiagnosticCodes.InvalidSpecificationCaseValue, $"Case '{row.Name}' parameter '{parameter.Name}' cannot be admitted: {failure.Message}", assignment.Source.Location);
                        }
                    }
                }
            }
            var slices = syntax.Modules.SelectMany(module => module.Features).SelectMany(AllSlices).ToArray();
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
                    var bound = _exampleCommands.GetValueOrDefault(command);
                    var identifier = bound?.Properties.SingleOrDefault(property => property.IsIdentifier);
                    destinationType = bound?.Destination?.Type ?? identifier?.Type;
                    generatedDestination = identifier?.IsGenerated == true;
                }
                else if (example.Stream is { } route)
                {
                    var target = ResolveRoute(route.EventSource, route.Stream, route.Location);
                    var eventContract = _eventDeclarations[(EventSyntax)resolved.Type].Contract.Id;
                    destinationType = target is { } resolvedRoute
                        ? SemanticEventRouting.FixtureSourceType(application, new(resolvedRoute.Source.Id, resolvedRoute.Stream.Id), eventContract, out _)
                        : null;
                }
                else
                {
                    var eventName = ((EventSyntax)resolved.Type).Name;
                    var useSlices = slices.Where(slice => slice.Specifications.Any(specification => expansion.Specifications.Any(origin =>
                        ReferenceEquals(origin.Effective, specification) && origin.Steps.Any(step => ReferenceEquals(step.Example, example))))).ToArray();
                    if (useSlices.Length == 0) useSlices = [.. slices.Where(slice => EventDeclarations.In(slice).Any(node => ReferenceEquals(node, resolved.Type)))];
                    var localTypes = useSlices.SelectMany(slice => slice.Commands).Select(command => _exampleCommands.GetValueOrDefault(command))
                        .OfType<SemanticCommand>().SelectMany(command => command.Produces.Where(produced => _events.TryGetValue(eventName, out var declaration) && produced.EventContract == declaration.Contract.Id)
                            .Select(produced => SemanticModelValidator.ProducedEventSourceType(command, produced))).OfType<SemanticTypeReference>().Distinct().ToArray();
                    var producerTypes = localTypes.Length == 1 ? localTypes : CommandEventSourceTypes(eventName);
                    if (producerTypes.Length != 1) producerTypes = ProducerEventSourceTypes(eventName, []);
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
