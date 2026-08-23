// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_reading_malformed_nested_contracts : Specification
{
    Exception[] _errors;

    void Because()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.IdentityCatalogBytes);
        var malformed = new[]
        {
            canonical.Replace("\"schema\":\"cratis.screenplay.semantic-identities\"", "\"schema\":\"cratis.screenplay.semantic-identities\",\"schema\":\"cratis.screenplay.semantic-identities\"", StringComparison.Ordinal),
            canonical.Replace("\"address\":{\"kind\":", "\"address\":{\"unknown\":true,\"kind\":", StringComparison.Ordinal),
            canonical.Replace("\"address\":{\"kind\":", "\"address\":{\"kind\":-1,\"originalKind\":", StringComparison.Ordinal),
            canonical.Replace("\"parts\":[{\"kind\":0", "\"parts\":[{\"kind\":99", StringComparison.Ordinal),
            canonical.Replace("\"origin\":\"persisted\"", "\"origin\":\"unknown\"", StringComparison.Ordinal),
            canonical.Replace("\"origin\":\"persisted\"", "\"origin\":\"persisted\",\"origin\":\"persisted\"", StringComparison.Ordinal)
        };
        _errors =
        [
            .. malformed.Select(json => Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize(Encoding.UTF8.GetBytes(json))))
        ];
    }

    [Fact] void should_reject_duplicate_unknown_and_mixed_nested_contracts() => _errors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
}
#endif
