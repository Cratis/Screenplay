// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_command_destination_types : Specification
{
    static IEnumerable<ExecutableSemanticModel> Models =>
    [
        canonical_serialization_golden_vectors.CreateSemanticModel(),
        canonical_serialization_golden_vectors.CreateSemanticModelV2(),
        canonical_serialization_golden_vectors.CreateSemanticModelV3(),
        canonical_serialization_golden_vectors.CreateSemanticModelV4(),
        canonical_serialization_golden_vectors.CreateSemanticModelV5(),
        canonical_serialization_golden_vectors.CreateSemanticModelV6()
    ];

    [Fact]
    void should_reject_reaction_metadata_on_commands_in_every_admitted_version()
    {
        foreach (var model in Models)
        {
            var canonical = Encoding.UTF8.GetString(SemanticModelCanonicalJson.SerializeWithoutRevision(model.LanguageVersion, model.SemanticVersion, model.Application));
            using var document = JsonDocument.Parse(canonical);
            var produced = Slices(document.RootElement.GetProperty("application").GetProperty("modules").EnumerateArray().First().GetProperty("features"))
                .SelectMany(slice => slice.GetProperty("commands").EnumerateArray()).SelectMany(command => command.GetProperty("produces").EnumerateArray()).First().GetRawText();
            var index = canonical.IndexOf(produced, StringComparison.Ordinal);
            var illegal = produced[..^1] + ""","destinationType":{"kind":"primitive","primitive":"string","target":null,"collection":false,"optional":false}}""";
            var content = string.Concat(canonical.AsSpan(0, index), illegal, canonical.AsSpan(index + produced.Length));

            // Hash the mutated canonical content, not the old model; refusal cannot be due to a stale revision.
            var revision = SemanticRevision.Compute(Encoding.UTF8.GetBytes(content));
            var json = content.Replace(",\"application\":", $",\"revision\":\"{revision}\",\"application\":", StringComparison.Ordinal);
            var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
            error.ShouldBeOfExactType<InvalidSemanticContract>();
            error.Message.Contains("revision", StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        }
    }

    [Fact]
    void should_reject_programmatic_command_destination_types_in_every_admitted_version()
    {
        foreach (var model in Models)
        {
            var illegal = model.Application with { Modules = [.. model.Application.Modules.Select(module => module with { Features = [.. module.Features.Select(Change)] })] };
            Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, illegal)).ShouldBeOfExactType<InvalidSemanticContract>();
            Catch.Exception(() => SemanticModelCanonicalJson.SerializeWithoutRevision(model.LanguageVersion, model.SemanticVersion, illegal)).ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }

    [Fact]
    void should_keep_valid_v6_reaction_destination_types() =>
        SemanticModelSerializer.Deserialize(canonical_serialization_golden_vectors.SemanticModelV6Bytes).Revision
            .ShouldEqual(canonical_serialization_golden_vectors.CreateSemanticModelV6().Revision);

    static SemanticFeature Change(SemanticFeature feature) => feature with
    {
        Features = [.. feature.Features.Select(Change)],
        Slices = [.. feature.Slices.Select(slice => slice with
        {
            Commands = [.. slice.Commands.Select(command => command with { Produces = [.. command.Produces.Select(produced => produced with { DestinationType = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text) })] })]
        })]
    };

    static IEnumerable<JsonElement> Slices(JsonElement features) => features.EnumerateArray().SelectMany(feature =>
        feature.GetProperty("slices").EnumerateArray().Concat(Slices(feature.GetProperty("features"))));
}
