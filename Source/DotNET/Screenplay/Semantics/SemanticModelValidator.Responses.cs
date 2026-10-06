// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal static partial class SemanticModelValidator
{
    static void ValidateResponseVersion(SemanticApplication application, SemanticVersion version)
    {
        var slices = application.Modules.SelectMany(module => module.Features).SelectMany(AllSlices);
        var uses = slices.Any(slice => slice.Commands.Any(command => command.Response is not null || command.Properties.Any(property => property.IsGenerated)) ||
            slice.Specifications.Any(specification => specification.ThenReturns is not null || specification.When is { GeneratedValues.IsDefaultOrEmpty: false }));
        if (version == SemanticVersion.V7 && !uses)
        {
            throw new InvalidSemanticContract("An ESM v7 model must contain a generated property, response, generation fixture or return expectation.");
        }

        if (!version.IsAtLeast(SemanticVersion.V7) && uses)
        {
            throw new InvalidSemanticContract("Generated properties, responses, generation fixtures and return expectations require ESM v7.");
        }
    }

    private sealed partial class ValidationContext
    {
        void ValidateResponse(SemanticCommand command)
        {
            switch (command.Response)
            {
                case null: return;
                case SemanticScalarCommandResponse scalar:
                    ValidateResponseSource(command, scalar.Source, scalar.Type);
                    break;
                case SemanticRecordCommandResponse record:
                    RequireObjects(record.Fields, nameof(record.Fields), "response field");
                    if (record.Fields.IsEmpty) throw new InvalidSemanticContract("A response record must contain fields.");
                    RejectDuplicateNames(record.Fields.Select(field => field.Name), "response field");
                    foreach (var field in record.Fields)
                    {
                        ValidateResponseSource(command, field.Source, field.Type);
                    }
                    break;
                default: throw new InvalidSemanticContract("Unknown command response variant.");
            }
        }

        void ValidateResponseSource(SemanticCommand command, SemanticId source, SemanticTypeReference type)
        {
            ValidateTypeReference(type);
            if (type.IsCollection || command.Properties.SingleOrDefault(property => property.Id == source) is not { } property || property.Type != type)
            {
                throw new InvalidSemanticContract("A response source must be a scalar property of its own command with exactly the same type and optionality.");
            }
        }

        void ValidateGeneratedValues(SemanticSpecificationCommand when, SemanticCommand command)
        {
            if (command.Properties.Any(property => property.IsGenerated && property.IsIdentifier) && when.EventSource is not null)
            {
                throw new InvalidSemanticContract("A generated identifier fixture belongs in generatedValues, never eventSource.");
            }

            // Fixtures can be partial: missing generation is an execution capability failure, not malformed ESM.
            ValidatePropertyValues(when.GeneratedValues, [.. command.Properties.Where(property => property.IsGenerated)], false);
        }

        void ValidateThenReturns(SemanticSpecification specification, SemanticCommand? command)
        {
            if (specification.ThenReturns is null) return;
            if (command?.Response is null || specification.ThenDenied || !specification.ThenErrors.IsEmpty)
            {
                throw new InvalidSemanticContract("A return expectation requires a command response and a success outcome.");
            }

            switch (specification.ThenReturns, command.Response)
            {
                case (SemanticScalarSpecificationResponse expected, SemanticScalarCommandResponse response):
                    ValidateValue(expected.Value, response.Type, "scalar return expectation");
                    break;
                case (SemanticRecordSpecificationResponse expected, SemanticRecordCommandResponse response):
                    RequireObjects(expected.Fields, nameof(expected.Fields), "return expectation field");
                    if (expected.Fields.IsEmpty) throw new InvalidSemanticContract("A record return expectation requires a non-empty subset of fields.");
                    RejectDuplicateNames(expected.Fields.Select(field => field.Name), "return expectation field");
                    foreach (var field in expected.Fields)
                    {
                        var source = response.Fields.SingleOrDefault(value => value.Name == field.Name) ??
                            throw new InvalidSemanticContract("A return expectation names an unknown response field.");
                        ValidateValue(field.Value, source.Type, "record return expectation");
                    }
                    break;
                default: throw new InvalidSemanticContract("A return expectation must match the scalar or record response shape.");
            }
        }
    }
}
