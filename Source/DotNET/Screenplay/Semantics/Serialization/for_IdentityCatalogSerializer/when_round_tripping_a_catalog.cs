// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_round_tripping_a_catalog : a_valid_semantic_model
{
    byte[] _json;
    byte[] _reserialized;
    SemanticIdentityCatalog _roundTripped;

    void Because()
    {
        _json = SemanticIdentityCatalogSerializer.Serialize(_catalog);
        _roundTripped = SemanticIdentityCatalogSerializer.Deserialize(_json);
        _reserialized = SemanticIdentityCatalogSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_preserve_semantic_assignments() => _roundTripped.Semantics.Single().Id.ShouldEqual(_applicationId);
    [Fact] void should_preserve_event_assignments() => _roundTripped.EventContracts.Single().Id.ShouldEqual(_eventContractId);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();
}
