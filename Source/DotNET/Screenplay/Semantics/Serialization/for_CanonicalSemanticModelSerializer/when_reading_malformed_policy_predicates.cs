// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_policy_predicates : Specification
{
    [Fact]
    void should_reject_a_nested_opaque_policy()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelV3Bytes);
        var malformed = canonical.Replace(
            "\"name\":\"RequiresTargetPolicy\",\"condition\":{\"kind\":\"opaque\"}",
            "\"name\":\"RequiresTargetPolicy\",\"condition\":{\"kind\":\"logical\",\"operator\":\"and\",\"left\":{\"kind\":\"opaque\"},\"right\":{\"kind\":\"authenticated\"}}",
            StringComparison.Ordinal);
        malformed.ShouldNotEqual(canonical);
        Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(malformed))).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_reject_noncanonical_policy_shapes()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelV3Bytes);
        const string policy = "{\"name\":\"RequiresTargetPolicy\",\"condition\":{\"kind\":\"opaque\"},\"requirementId\":\"";
        canonical.Contains(policy, StringComparison.Ordinal).ShouldBeTrue();
        var cases = new[]
        {
            canonical.Replace("\"kind\":\"opaque\"},\"requirementId\"", "\"kind\":\"opaque\",\"role\":\"Admin\"},\"requirementId\"", StringComparison.Ordinal),
            canonical.Replace("\"kind\":\"opaque\"},\"requirementId\"", "\"kind\":\"opaque\"},\"extra\":true,\"requirementId\"", StringComparison.Ordinal),
            canonical.Replace("\"kind\":\"opaque\"},\"requirementId\":\"" + new string('e', 64) + "\"", "\"kind\":\"opaque\"}", StringComparison.Ordinal),
            canonical.Replace("\"name\":\"RequiresTargetPolicy\",\"condition\"", "\"name\":\"RequiresTargetPolicy\",\"requirementId\":\"duplicate\",\"condition\"", StringComparison.Ordinal)
        };
        foreach (var json in cases)
        {
            Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))).ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }
}
#endif
