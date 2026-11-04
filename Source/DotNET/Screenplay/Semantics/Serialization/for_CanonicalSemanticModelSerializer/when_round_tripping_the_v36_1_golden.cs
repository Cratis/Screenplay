// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_the_v36_1_golden : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;
    UnknownPreservingSemanticDocument _promoted;
    byte[] _opaqueBytes;

    void Because()
    {
        golden_vector_regeneration.RegenerateWhenRequested();
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV36_1();
        _bytes = SemanticModelSerializer.Serialize(_model);
        _promoted = UnknownPreservingSemanticModelReader.Read(_bytes, new(36, 0, 1));
        using var document = JsonDocument.Parse(_bytes);
        _opaqueBytes = new EsmAdmissionCanonicalJson(document.RootElement, 36, 0).Write(projection: false, _model.Revision);
    }

    [Fact] void should_pin_the_minor_golden() => _bytes.ShouldEqual(canonical_serialization_golden_vectors.EsmV36_1Bytes);
    [Fact] void should_round_trip_the_typed_member() => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(_bytes)).ShouldEqual(_bytes);
    [Fact] void should_keep_opaque_and_typed_wire_bytes_identical() => _opaqueBytes.ShouldEqual(_bytes);

    [Fact]
    void should_read_every_property_order_identity()
    {
        var restored = SemanticModelSerializer.Deserialize(_bytes).Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
        var original = _model.Application.Modules[0].Features[0].Slices.SelectMany(slice => slice.ReadModels).Single();
        SemanticId[] expected = [.. original.PropertyOrder];
        restored.PropertyOrder.ShouldEqual(expected);
    }
    [Fact] void should_keep_the_full_revision_on_promotion() => _promoted.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_the_full_bytes_on_promotion() => _promoted.CopyBytes().ShouldEqual(_bytes);
    [Fact] void should_not_report_the_promoted_member_as_unknown() => _promoted.Unknowns.ShouldBeEmpty();
    [Fact] void should_retain_the_minor_in_the_typed_projection() => _promoted.Model.SemanticVersion.ShouldEqual(SemanticVersion.V36_1);

    [Fact]
    void should_leave_36_0_as_a_valid_projection()
    {
        using var document = JsonDocument.Parse(_bytes);
        var projection = new EsmAdmissionCanonicalJson(document.RootElement, 36, 0).Write(projection: true, null);
        var revision = SemanticRevision.Compute(projection);
        var sealedProjection = new EsmAdmissionCanonicalJson(document.RootElement, 36, 0).Write(projection: true, revision);
        var known = SemanticModelSerializer.Deserialize(sealedProjection);
        SemanticModelSerializer.Serialize(known).ShouldEqual(SemanticModelSerializer.Serialize(property_order_binding.BindDefault().Value!.Model));
    }
}
