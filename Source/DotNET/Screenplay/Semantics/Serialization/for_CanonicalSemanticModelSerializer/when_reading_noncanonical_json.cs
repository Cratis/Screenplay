// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_noncanonical_json : Specification
{
    Exception[] _errors;
    Exception[] _writeErrors;

    void Because()
    {
        var canonical = canonical_serialization_golden_vectors.SemanticModelBytes;
        var text = Encoding.UTF8.GetString(canonical);
        var invalidUtf8 = canonical.ToArray();
        invalidUtf8[Array.IndexOf(invalidUtf8, (byte)'C')] = 0xff;
        var nonNfc = Encoding.UTF8.GetBytes(text.Replace("caf\\u00E9", "cafe\u0301", StringComparison.Ordinal));
        var nonCanonicalUnicodeEncoding = Encoding.UTF8.GetBytes(text.Replace("caf\\u00E9", "café", StringComparison.Ordinal));
        var trailingData = canonical.Concat(Encoding.UTF8.GetBytes("{}")).ToArray();
        byte[] leadingWhitespace = [(byte)' ', .. canonical];
        var trailingWhitespace = canonical.Append((byte)'\n').ToArray();
        var internalWhitespace = Encoding.UTF8.GetBytes(text.Replace("{\"schema\"", "{ \"schema\"", StringComparison.Ordinal));
        _errors =
        [
            .. new[] { invalidUtf8, nonNfc, nonCanonicalUnicodeEncoding, trailingData, leadingWhitespace, trailingWhitespace, internalWhitespace }
                .Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(json)))
        ];

        var model = canonical_serialization_golden_vectors.CreateSemanticModel();
        var textConcept = model.Application.Concepts.Single(_ => _.Name == "Label");
        var nonNfcApplication = model.Application with
        {
            Concepts = model.Application.Concepts.Replace(textConcept, textConcept with { Values = ["cafe\u0301"] })
        };
        var malformedUtf16Application = model.Application with { Name = "\ud800" };
        _writeErrors =
        [
            Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, nonNfcApplication)),
            Catch.Exception(() => ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, malformedUtf16Application))
        ];
    }

    [Fact] void should_reject_invalid_utf8_non_nfc_trailing_data_and_whitespace() => _errors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_reject_non_nfc_and_malformed_utf16_models_as_invalid_contracts() => _writeErrors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
}
#endif
