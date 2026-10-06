// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] SemanticModelV7Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v7.json");

    public static ExecutableSemanticModel CreateSemanticModelV7()
    {
        var v6 = CreateSemanticModelV6();
        var concept = new SemanticConcept(Id(8000), "GeneratedIdentity", SemanticPrimitiveType.Uuid, [], []);
        var identity = SemanticTypeReference.ForConcept(concept.Id);
        var note = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isOptional: true);
        var id = new SemanticProperty(Id(8011), "Id", identity, true) { IsGenerated = true };
        var token = new SemanticProperty(Id(8021), "Token", identity, false) { IsGenerated = true };
        var optional = new SemanticProperty(Id(8022), "Note", note, false);
        var created = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(8030), "GeneratedEntityCreated", [new(Id(8031), "Id", identity, false)]);
        var scalar = new SemanticCommand(
            Id(8010),
            "CreateEntity",
            [id],
            [],
            [
                new(created.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, id.Id), [new(Id(8031), SemanticExpression.Property(SemanticExpressionRootKind.Command, id.Id))])
            ])
        {
            Destination = new(identity, SemanticExpression.Property(SemanticExpressionRootKind.Command, id.Id)),
            Response = new SemanticScalarCommandResponse(id.Id, identity)
        };
        var record = new SemanticCommand(Id(8020), "CreateToken", [optional, token], [], [])
        {
            // Deliberately not alphabetical: response order is authored, expectation order is canonical.
            Response = new SemanticRecordCommandResponse([new("token", identity, token.Id), new("note", note, optional.Id)])
        };
        var nullable = new SemanticCommand(Id(8040), "ReturnNote", [new(Id(8041), "Note", note, false)], [], [])
        {
            Response = new SemanticScalarCommandResponse(Id(8041), note)
        };
        var generated = SemanticValue.Text("0b4f8e6c-1d6a-4a52-9a53-3f5b6a0c1d11");
        var slice = new SemanticSlice(
            Id(8050),
            "Responses",
            SemanticSliceKind.StateChange,
            [created],
            [record, scalar, nullable],
            [],
            [],
            [],
            [
                Specification(Id(8060), "returns a generated identifier") with
                {
                    When = new(scalar.Id, []) { GeneratedValues = [new(id.Id, generated)] },
                    ThenEvents = [new(created.Id, [new(Id(8031), generated)]) { EventSource = new(identity, generated) }],
                    ThenReturns = new SemanticScalarSpecificationResponse(generated)
                },
                Specification(Id(8061), "returns a record with a null field") with
                {
                    When = new(record.Id, [new(optional.Id, SemanticValue.Null)]) { GeneratedValues = [new(token.Id, generated)] },
                    ThenReturns = new SemanticRecordSpecificationResponse([new("token", generated), new("note", SemanticValue.Null)])
                },
                Specification(Id(8062), "returns null") with
                {
                    When = new(nullable.Id, [new(Id(8041), SemanticValue.Null)]),
                    ThenReturns = new SemanticScalarSpecificationResponse(SemanticValue.Null)
                }
            ]);
        var modules = v6.Application.Modules.Select((module, index) => index == 0
            ? module with { Features = module.Features.Add(new SemanticFeature(Id(8070), "GeneratedResponses", [], [slice])) }
            : module).ToImmutableArray();
        return ExecutableSemanticModel.Create(LanguageVersion.V7, SemanticVersion.V7, v6.Application with { Concepts = v6.Application.Concepts.Add(concept), Modules = modules });
    }
}
#endif
