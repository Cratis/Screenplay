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
        SemanticSpecificationCommand BindSpecificationCommand(SpecificationCommandSyntax when, SemanticCommand command)
        {
            var properties = command.Properties.ToDictionary(property => property.Name, StringComparer.Ordinal);
            var inputs = new List<PropertyMappingSyntax>();
            foreach (var value in when.Values)
            {
                if (properties.GetValueOrDefault(value.Property) is { IsGenerated: true })
                {
                    Error(DiagnosticCodes.GeneratedPropertySuppliedAsInput, $"Specification command '{command.Name}' cannot supply generated property '{value.Property}' as request input; use 'for' for its generated identifier or 'generated' for another generated value.", value.Location);
                }
                else
                {
                    inputs.Add(value);
                }
            }

            var generated = ImmutableArray.CreateBuilder<SemanticPropertyValue>();
            var identifier = command.Properties.SingleOrDefault(property => property.IsIdentifier && property.IsGenerated);
            if (identifier is not null && when.For is not null && BindGeneratedValue(when.For, identifier) is { } fixture)
            {
                generated.Add(new(identifier.Id, fixture));
            }

            foreach (var value in when.GeneratedValues)
            {
                UsesV7 = true;
                if (properties.GetValueOrDefault(value.Property) is not { IsGenerated: true, IsIdentifier: false } property)
                {
                    Error(DiagnosticCodes.InvalidSemanticBinding, $"Generation fixture '{value.Property}' must target a nonidentifier generated property of command '{command.Name}'; use 'for' for a generated identifier.", value.Location);
                    continue;
                }

                if (BindGeneratedValue(value.Source, property) is { } generatedFixture) generated.Add(new(property.Id, generatedFixture));
            }

            return new(command.Id, BindPropertyValues(inputs, properties.Where(entry => !entry.Value.IsGenerated).ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal), "specification command"))
            {
                GeneratedValues = generated.ToImmutable(),
                EventSource = identifier is not null || when.For is null ? null : BindEventSource(when.For, command.Destination?.Type ?? command.Properties.SingleOrDefault(property => property.IsIdentifier)?.Type)
            };
        }

        SemanticValue? BindGeneratedValue(ExpressionSyntax expression, SemanticProperty property)
        {
            if (expression is LiteralExpressionSyntax { Value: string text } && Guid.TryParse(text, out var uuid))
            {
                return SemanticValue.Text(uuid.ToString("D"));
            }

            Error(DiagnosticCodes.InvalidSemanticBinding, $"Generated fixture or return expectation for '{property.Name}' requires a UUID value; use a quoted UUID such as '11111111-1111-1111-1111-111111111111'.", expression.Location);
            return null;
        }

        SemanticSpecificationResponse? BindThenReturns(SpecificationReturnSyntax? expected, SemanticCommand? command)
        {
            if (expected is null) return null;
            UsesV7 = true;
            switch (expected, command?.Response)
            {
                case (ScalarSpecificationReturnSyntax scalar, SemanticScalarCommandResponse response):
                    return BindReturnValue(scalar.Value, command, response.Source, response.Type) is { } value
                        ? new SemanticScalarSpecificationResponse(value) : null;
                case (RecordSpecificationReturnSyntax record, SemanticRecordCommandResponse response):
                    var fields = ImmutableArray.CreateBuilder<SemanticSpecificationResponseField>();
                    foreach (var field in record.Fields)
                    {
                        var source = response.Fields.SingleOrDefault(value => value.Name == field.Property);
                        if (source is null)
                        {
                            Error(DiagnosticCodes.InvalidSemanticBinding, $"Return expectation field '{field.Property}' is not declared by command '{command.Name}'.", field.Location);
                            continue;
                        }

                        if (BindReturnValue(field.Source, command, source.Source, source.Type) is { } fieldValue) fields.Add(new(field.Property, fieldValue));
                    }

                    return new SemanticRecordSpecificationResponse(fields.ToImmutable());
                default:
                    Error(DiagnosticCodes.InvalidSemanticBinding, "A return expectation requires a command action and must match its scalar or record response.", expected.Location);
                    return null;
            }
        }

        SemanticValue? BindReturnValue(ExpressionSyntax expression, SemanticCommand command, SemanticId source, SemanticTypeReference type) =>
            command.Properties.Single(property => property.Id == source).IsGenerated
                ? BindGeneratedValue(expression, command.Properties.Single(property => property.Id == source))
                : BindConcreteValue(expression, type, "return expectation", true);
    }
}
