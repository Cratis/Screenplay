// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_value_variants : Specification
{
    Exception[] _shapeErrors;
    Exception _nestedNonCanonicalDecimal;
    Exception _nestedNonNfcText;

    void Because()
    {
        var malformed = new[]
        {
            /*lang=json,strict*/
                                 "{\"kind\":\"unknown\"}",
            /*lang=json,strict*/
                                 "{\"kind\":\"array\",\"kind\":\"array\",\"values\":[]}",
            /*lang=json,strict*/
                                 "{\"values\":[],\"kind\":\"array\"}",
            /*lang=json,strict*/
                                 "{\"kind\":\"array\",\"value\":true}",
            /*lang=json,strict*/
                                 "{\"kind\":\"array\",\"values\":[],\"properties\":[]}",
            /*lang=json,strict*/
                                 "{\"kind\":\"array\",\"values\":[null]}",
            /*lang=json,strict*/
                                 "{\"kind\":\"array\",\"values\":[true]}",
            /*lang=json,strict*/
                                 "{\"kind\":\"object\",\"properties\":[],\"properties\":[]}",
            /*lang=json,strict*/
                                 "{\"kind\":\"object\",\"properties\":[{\"targetProperty\":\"sem1:0000000000000000000000000000000000000000000000000000000000000001\"}]}"
        };
        _shapeErrors = [.. malformed.Select(ReadValue)];

        var canonical = Encoding.UTF8.GetString(given.canonical_serialization_golden_vectors.SemanticModelBytes);
        _nestedNonCanonicalDecimal = Catch.Exception(() => SemanticModelSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace("\"kind\":\"number\",\"value\":2}", "\"kind\":\"number\",\"value\":2.0}", StringComparison.Ordinal))));
        _nestedNonNfcText = Catch.Exception(() => SemanticModelSerializer.Deserialize(
            Encoding.UTF8.GetBytes(canonical.Replace("\"kind\":\"string\",\"value\":\"second\"", "\"kind\":\"string\",\"value\":\"Cafe\\u0301\"", StringComparison.Ordinal))));
    }

    [Fact] void should_reject_unknown_mixed_duplicate_misordered_and_malformed_nested_shapes() => _shapeErrors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_reject_a_noncanonical_nested_decimal() => _nestedNonCanonicalDecimal.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_non_nfc_nested_text() => _nestedNonNfcText.ShouldBeOfExactType<InvalidSemanticContract>();

    static Exception ReadValue(string json) => Catch.Exception(() =>
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json), CanonicalJson.ReaderOptions);
        SemanticModelRead.RequiredToken(ref reader, JsonTokenType.StartObject, "value");
        _ = SemanticModelRead.Value(ref reader);
        if (reader.Read())
        {
            throw new InvalidSemanticContract("The value contains trailing data.");
        }
    });
}
#endif
