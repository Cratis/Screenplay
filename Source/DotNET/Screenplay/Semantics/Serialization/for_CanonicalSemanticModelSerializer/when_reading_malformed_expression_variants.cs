// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_expression_variants : Specification
{
    Exception[] _errors;

    void Because()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelBytes);
        var malformed = new[]
        {
            canonical.Replace("\"kind\":\"resolved\",\"root\":", "\"kind\":\"unknown\",\"root\":", StringComparison.Ordinal),
            canonical.Replace("\"root\":\"command\"", "\"root\":\"unknown\"", StringComparison.Ordinal),
            canonical.Replace("\"source\":\"property\"", "\"source\":\"unknown\"", StringComparison.Ordinal),
            canonical.Replace("\"root\":\"command\"", "\"root\":\"query\"", StringComparison.Ordinal),
            canonical.Replace("\"source\":\"property\"", "\"source\":\"argument\"", StringComparison.Ordinal),
            canonical.Replace("\"kind\":\"resolved\",\"root\":", "\"kind\":\"resolved\",\"kind\":\"resolved\",\"root\":", StringComparison.Ordinal),
            canonical.Replace("\"kind\":\"resolved\",\"root\":", "\"kind\":\"resolved\",\"unexpected\":true,\"root\":", StringComparison.Ordinal),
            canonical.Replace("\"kind\":\"resolved\",\"root\":", "\"kind\":\"resolved\",\"value\":{\"kind\":\"null\"},\"root\":", StringComparison.Ordinal)
        };
        _errors =
        [
            .. malformed.Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))))
        ];
    }

    [Fact] void should_reject_every_unknown_mixed_or_duplicate_expression_shape() => _errors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
}
#endif
