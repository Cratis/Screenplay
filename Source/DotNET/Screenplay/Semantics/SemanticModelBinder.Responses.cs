// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

public sealed partial class SemanticModelBinder
{
    private sealed partial class BindingContext
    {
        static IEnumerable<string> ConditionPaths(ConditionSyntax condition) => condition switch
        {
            LogicalConditionSyntax logical => ConditionPaths(logical.Left).Concat(ConditionPaths(logical.Right)),
            ComparisonConditionSyntax { Right: PathExpressionSyntax path } comparison => [comparison.Left, path.Path],
            ComparisonConditionSyntax comparison => [comparison.Left],
            _ => []
        };

        void ValidateGeneratedProperties(CommandSyntax command, Dictionary<string, SemanticProperty> properties)
        {
            foreach (var source in command.Properties.Where(property => property.IsGenerated))
            {
                var property = properties[source.Name];
                var concept = syntax.Concepts.SingleOrDefault(value => _concepts[value.Name].Id == property.Type.Target);
                if (property.Type is not { Kind: SemanticTypeReferenceKind.Concept, IsCollection: false, IsOptional: false } || concept is null || (concept.Type != "Uuid" && concept.Generation is null))
                {
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Generated property '{property.Name}' of command '{command.Name}' requires a required scalar generatable concept; declare a generation strategy or use a UUID concept.", source.Location);
                }
                else
                {
                    ValidateGenerationEntropy(command, source, concept);
                }
            }
        }

        void GeneratedReference(string command, string property, string role, SourceLocation location) =>
            Error(DiagnosticCodes.InvalidSemanticBinding, $"Generated property '{property}' of command '{command}' cannot be used by {role}; generated values exist only after validation. Reference an input property or remove 'generated'.", location);

        SemanticCondition? BindPreGenerationCondition(CommandSyntax command, ConditionSyntax condition, Dictionary<string, SemanticProperty> properties)
        {
            var generated = ConditionPaths(condition).Select(path => properties.GetValueOrDefault(path.Split('.')[0]))
                .FirstOrDefault(property => property?.IsGenerated == true);
            if (generated is not null)
            {
                GeneratedReference(command.Name, generated.Name, "declarative requirement", condition.Location);
                return null;
            }

            return BindCondition(condition, properties, allowReadPaths: syntax.CommandReadMode is not null, position: "require");
        }

        SemanticCommandResponse? BindResponse(CommandSyntax command, Dictionary<string, SemanticProperty> properties)
        {
            switch (command.Response)
            {
                case null: return null;
                case ScalarCommandResponseSyntax scalar:
                    return ResponseSource(command, scalar.Source, properties) is { } property
                        ? new SemanticScalarCommandResponse(property.Id, property.Type) : null;
                case RecordCommandResponseSyntax record:
                    var fields = ImmutableArray.CreateBuilder<SemanticCommandResponseField>();
                    foreach (var field in record.Fields)
                    {
                        if (ResponseSource(command, field.Source, properties) is not { } source) continue;
                        if (field.Type is not null && BindTypeReference(field.Type) != source.Type)
                        {
                            Error(DiagnosticCodes.InvalidSemanticBinding, $"Response field '{field.Name}' of command '{command.Name}' must have the same type as source '{source.Name}'.", field.Location);
                            continue;
                        }

                        fields.Add(new(field.Name, source.Type, source.Id));
                    }

                    return new SemanticRecordCommandResponse(fields.ToImmutable());
                default:
                    Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Command '{command.Name}' has an unsupported response shape.", command.Response.Location);
                    return null;
            }
        }

        SemanticProperty? ResponseSource(CommandSyntax command, PropertyResponseSourceSyntax source, Dictionary<string, SemanticProperty> properties)
        {
            if (syntax.CommandReadMode is not null && source.Property.Contains('.'))
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Read path '{source.Property}' is not admitted in response mappings.", source.Location);
                return null;
            }
            if (!properties.TryGetValue(source.Property, out var property))
            {
                Error(DiagnosticCodes.InvalidSemanticBinding, $"Response source '{source.Property}' is not a property of command '{command.Name}'.", source.Location);
                return null;
            }

            if (property.Type.IsCollection)
            {
                Error(DiagnosticCodes.UnsupportedSemanticSyntax, $"Response source '{source.Property}' of command '{command.Name}' is a collection; use a scalar property.", source.Location);
                return null;
            }

            return property;
        }
    }
}
