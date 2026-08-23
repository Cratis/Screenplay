// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_reading_a_hostile_address_shape : a_valid_semantic_model
{
    Exception _exception;

    void Because()
    {
        var slice = SemanticAddress.ForSlice(_applicationIdentity, "Projects", "Projects", "Registration");
        var property = SemanticAddress.ForProperty(SemanticAddress.ForCommand(slice, "RegisterProject"), "Id");
        var catalog = SemanticIdentityCatalog.Create(
            _applicationIdentity,
            [],
            [new(property, SemanticId.Create(property), SemanticIdentityOrigin.LegacyBootstrap)],
            []);
        var json = Encoding.UTF8.GetString(SemanticIdentityCatalogSerializer.Serialize(catalog));
        var hostile = json.Replace("\"kind\":7,\"key\":\"7\"", "\"kind\":7,\"key\":\"10\"", StringComparison.Ordinal);
        _exception = Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(hostile)));
    }

    [Fact] void should_reject_the_illegal_property_owner_kind() => _exception.ShouldBeOfExactType<InvalidSemanticContract>();
}
