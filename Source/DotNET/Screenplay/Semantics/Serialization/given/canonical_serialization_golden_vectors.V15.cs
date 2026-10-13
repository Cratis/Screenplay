// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.Serialization.given;

public static partial class canonical_serialization_golden_vectors
{
    public static byte[] EsmV15Bytes => ReadResource("Cratis.Screenplay.Semantics.Serialization.Golden.full-esm-v15.json");

    public static ExecutableSemanticModel CreateSemanticModelV15()
    {
        var application = CreateSemanticModelV14().Application;
        SemanticGeneration[] strategies =
        [
            new("uuidv7"), new("uuid") { Prefix = "ref_", Case = "upper" },
            new("ulid") { Prefix = "cus_", Case = "lower" }, new("typeid") { Prefix = "user" }, new("typeid") { Prefix = "" },
            new("nanoid") { Size = 21 }, new("nanoid") { Size = 64, Alphabet = "ab", Prefix = "code_" },
            new("bytes") { Count = 32, Encoding = "base64url" }, new("bytes") { Count = 16, Encoding = "hex", Case = "upper" },
            new("bytes") { Count = 16, Encoding = "base32", Case = "lower" }
        ];
        var concepts = strategies.Select((strategy, index) => new SemanticConcept(Id(10500 + index), $"Generation{index}", index == 0 ? SemanticPrimitiveType.Uuid : SemanticPrimitiveType.Text, [], []) { Generation = strategy }).ToArray();
        var identity = SemanticTypeReference.ForConcept(concepts[0].Id);
        var code = SemanticTypeReference.ForConcept(concepts[5].Id);
        var id = new SemanticProperty(Id(10521), "Id", identity, true) { IsGenerated = true };
        var receipt = new SemanticProperty(Id(10522), "Receipt", code, false) { IsGenerated = true };
        var created = Event(ApplicationIdentity.Create("Canonical Golden Application"), Id(10530), "StrategyEntityCreated", [new(Id(10531), "Receipt", code, false)]);
        var command = new SemanticCommand(Id(10520), "CreateStrategyEntity", [id, receipt], [],
            [new(created.Id, null, SemanticExpression.Property(SemanticExpressionRootKind.Command, id.Id), [new(Id(10531), SemanticExpression.Property(SemanticExpressionRootKind.Command, receipt.Id))])])
        {
            Destination = new(identity, SemanticExpression.Property(SemanticExpressionRootKind.Command, id.Id))
        };
        var generated = SemanticValue.Text("00112233-4455-7677-8899-aabbccddeeff");
        var receiptValue = SemanticValue.Text("ABCDEFGHIJKLMNOPQRSTU");
        var fact = new SemanticSpecificationEvent(created.Id, [new(Id(10531), receiptValue)]) { EventSource = new(identity, generated) };
        var specification = Specification(Id(10540), "generated source cannot be reused") with
        {
            GivenEvents = [fact],
            When = new(command.Id, []) { GeneratedValues = [new(id.Id, generated), new(receipt.Id, receiptValue)] },
            ThenErrors = [new(null, FreshnessMessage())]
        };
        var slice = new SemanticSlice(Id(10550), "StrategyGeneration", SemanticSliceKind.StateChange, [created], [command], [], [], [], [specification]);
        var module = application.Modules[0];
        application = application with
        {
            Concepts = application.Concepts.AddRange(concepts),
            Modules = application.Modules.SetItem(0, module with { Features = module.Features.Add(new(Id(10560), "GenerationStrategies", [], [slice])) })
        };
        return ExecutableSemanticModel.Create(LanguageVersion.V15, SemanticVersion.V15, application);
    }

    static string FreshnessMessage() => SemanticCommandExecution.FreshnessMessage;
}
#endif
