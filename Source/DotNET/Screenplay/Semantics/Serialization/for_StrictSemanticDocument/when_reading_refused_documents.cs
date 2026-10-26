// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_StrictSemanticDocument;

public class when_reading_refused_documents : Specification
{
    byte[][] _cases;
    Exception?[] _strict;
    Exception?[] _boundary;

    void Establish()
    {
        var canonical = canonical_serialization_golden_vectors.SemanticModelBytes;
        var text = Encoding.UTF8.GetString(canonical);
        using var json = JsonDocument.Parse(canonical);
        var revision = json.RootElement.GetProperty("revision").GetString()!;
        var invalidUtf8 = canonical.ToArray();
        invalidUtf8[Array.IndexOf(invalidUtf8, (byte)'C')] = 0xff;
        _cases =
        [
            [],
            Encoding.UTF8.GetBytes(text.Replace("\"application\":", "\"unknown\":true,\"application\":", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"schema\":\"cratis.screenplay.esm\"", "\"schema\":\"cratis.screenplay.esm\",\"schema\":\"cratis.screenplay.esm\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace(revision, "rev1:0000000000000000000000000000000000000000000000000000000000000000", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"languageVersion\":\"1.0\"", "\"languageVersion\":\"1.1\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"languageVersion\":\"1.0\"", "\"languageVersion\":\"2.0\"", StringComparison.Ordinal)),
            Encoding.UTF8.GetBytes(text.Replace("\"semanticVersion\":\"1.0\"", "\"semanticVersion\":\"2.0\"", StringComparison.Ordinal)),
            [(byte)' ', .. canonical],
            [.. canonical, (byte)'{', (byte)'}'],
            invalidUtf8,
            Encoding.UTF8.GetBytes(text.Replace("caf\\u00E9", "cafe\u0301", StringComparison.Ordinal))
        ];
    }

    void Because()
    {
        _strict = [.. _cases.Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(json)))];
        _boundary = [.. _cases.Select(json => Catch.Exception(() => StrictSemanticDocument.Read(json)))];
    }

    [Fact] void should_have_every_case_refused_by_the_strict_reader() => _strict.All(error => error is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_refuse_every_case() => _boundary.All(error => error?.GetType() == typeof(InvalidSemanticContract)).ShouldBeTrue();
    [Fact] void should_pass_the_strict_messages_through_unchanged() => _boundary.Select(error => error?.Message).ShouldEqual(_strict.Select(error => error?.Message));
}
