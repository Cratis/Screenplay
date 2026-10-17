// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_stored_event_names : Specification
{
    [Fact]
    void should_pin_v16_bytes_and_revision()
    {
        var bytes = canonical_serialization_golden_vectors.EsmV16Bytes;
        var read = SemanticModelSerializer.Deserialize(bytes);
        read.SemanticVersion.ShouldEqual(SemanticVersion.V16);
        SemanticModelSerializer.Serialize(read).SequenceEqual(bytes).ShouldBeTrue();
    }

    [Fact]
    void should_refuse_a_stored_name_in_a_frozen_v15_reader()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelV15()));
        json = json.Replace("\"name\":\"StrategyEntityCreated\"", "\"name\":\"StrategyEntityCreated\",\"storedName\":\"Original\"", StringComparison.Ordinal);
        var error = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json)));
        error.ShouldBeOfExactType<InvalidSemanticContract>();
        error!.Message.ShouldContain("storedName");
    }

    [Fact]
    void should_refuse_v16_without_a_serialized_stored_name()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV15().Application;
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V16, SemanticVersion.V16, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_v16_when_the_only_stored_name_equals_the_display_name()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV16().Application;
        var module = application.Modules[0];
        var feature = module.Features.Single(value => value.Slices.Any(slice => slice.Events.Any(contract => contract.StoredName is not null)));
        var slice = feature.Slices[0];
        var contract = slice.Events[0] with { StoredName = slice.Events[0].Name };
        slice = slice with { Events = slice.Events.SetItem(0, contract) };
        feature = feature with { Slices = feature.Slices.SetItem(0, slice) };
        module = module with { Features = [.. module.Features.Select(value => value.Id == feature.Id ? feature : value)] };
        application = application with { Modules = application.Modules.SetItem(0, module) };
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V16, SemanticVersion.V16, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_not_admit_pins_to_a_frozen_semantic_version()
    {
        var application = canonical_serialization_golden_vectors.CreateSemanticModelV16().Application;
        Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V15, SemanticVersion.V15, application)).ShouldBeOfExactType<InvalidSemanticContract>();
    }
}
