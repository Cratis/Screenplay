// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_expression_variants : a_valid_semantic_model
{
    Exception _mixedVariant;
    Exception _unknownDiscriminator;

    void Because()
    {
        var json = Encoding.UTF8.GetString(SemanticModelSerializer.Serialize(_model));
        var unknown = json.Replace("\"kind\":\"resolved\"", "\"kind\":\"unknown\"", StringComparison.Ordinal);
        var mixed = json.Replace(
            "\"kind\":\"resolved\",\"root\":",
            "\"kind\":\"resolved\",\"value\":{\"kind\":\"null\"},\"root\":",
            StringComparison.Ordinal);
        _unknownDiscriminator = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(unknown)));
        _mixedVariant = Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(mixed)));
    }

    [Fact] void should_reject_an_unknown_discriminator() => _unknownDiscriminator.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_mixed_variant_fields() => _mixedVariant.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
