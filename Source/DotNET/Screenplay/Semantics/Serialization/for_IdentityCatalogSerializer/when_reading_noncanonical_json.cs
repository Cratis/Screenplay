// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_IdentityCatalogSerializer;

public class when_reading_noncanonical_json : Specification
{
    Exception[] _errors;

    void Because()
    {
        var canonical = canonical_serialization_golden_vectors.IdentityCatalogBytes;
        var text = Encoding.UTF8.GetString(canonical);
        var invalidUtf8 = canonical.ToArray();
        invalidUtf8[Array.IndexOf(invalidUtf8, (byte)'z')] = 0xff;
        var nonNfc = Encoding.UTF8.GetBytes(text.Replace("z-caf\\u00E9.play", "z-cafe\u0301.play", StringComparison.Ordinal));
        var nonCanonicalUnicodeEncoding = Encoding.UTF8.GetBytes(text.Replace("z-caf\\u00E9.play", "z-café.play", StringComparison.Ordinal));
        var trailingData = canonical.Concat(Encoding.UTF8.GetBytes("{}")).ToArray();
        byte[] leadingWhitespace = [(byte)' ', .. canonical];
        var trailingWhitespace = canonical.Append((byte)'\n').ToArray();
        var internalWhitespace = Encoding.UTF8.GetBytes(text.Replace("{\"schema\"", "{ \"schema\"", StringComparison.Ordinal));
        var nonCanonicalNumber = Encoding.UTF8.GetBytes(text.Replace("\"schemaVersion\":1", "\"schemaVersion\":1.0", StringComparison.Ordinal));
        var excessiveDepth = Encoding.UTF8.GetBytes($"{new string('[', CanonicalJson.MaximumDepth + 1)}0{new string(']', CanonicalJson.MaximumDepth + 1)}");
        _errors =
        [
            .. new[] { invalidUtf8, nonNfc, nonCanonicalUnicodeEncoding, trailingData, leadingWhitespace, trailingWhitespace, internalWhitespace, nonCanonicalNumber, excessiveDepth }
                .Select(json => Catch.Exception(() => SemanticIdentityCatalogSerializer.Deserialize(json)))
        ];
    }

    [Fact] void should_reject_invalid_utf8_non_nfc_trailing_data_whitespace_and_excessive_depth() => _errors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
}
#endif
