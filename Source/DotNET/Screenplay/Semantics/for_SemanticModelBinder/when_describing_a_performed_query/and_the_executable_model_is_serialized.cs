// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_describing_a_performed_query;

public class and_the_executable_model_is_serialized : given.a_performed_query
{
    // The canonical bytes measured before the performed query shape existed: the shape lives only on the requirement.
    const string CanonicalBytesBeforeTheShape = "3289e7aab6f9b4a3698a5b2d3f7a5c584bf335abb24bb641c99534ee65ce4ec9";

    string _hash;

    void Because() => _hash = Convert.ToHexStringLower(SHA256.HashData(SemanticModelSerializer.Serialize(_result.Value!.Model)));

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_keep_the_canonical_bytes() => _hash.ShouldEqual(CanonicalBytesBeforeTheShape);
}
