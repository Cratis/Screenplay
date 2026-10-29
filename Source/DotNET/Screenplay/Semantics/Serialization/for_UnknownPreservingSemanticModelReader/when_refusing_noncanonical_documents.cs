// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_refusing_noncanonical_documents : Specification
{
    Exception?[] _errors;

    void Because()
    {
        var root = given.documents.Minimal();
        root["application"]!["future"] = JsonNode.Parse("{\"a\":1,\"z\":null}");
        var canonical = Encoding.UTF8.GetString(given.documents.Seal(root));
        var malformed = new[]
        {
            canonical.Replace("\"a\":1,\"z\":null", "\"z\":null,\"a\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":1.0", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":1e0", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":-0", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":1,\"a\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"A\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a-b\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", $"\"{new string('a', 65)}\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"$type\":1", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":\"cafe\\u0301\"", StringComparison.Ordinal),
            canonical.Replace("\"a\":1", "\"a\":\"\\u0061\"", StringComparison.Ordinal),
            canonical.Replace("\"future\":", "\"Future\":", StringComparison.Ordinal),
            canonical.Replace("\"schema\":", " \"schema\":", StringComparison.Ordinal),
            canonical.Replace("\"modules\":[],\"future\":{\"a\":1,\"z\":null}", "\"future\":{\"a\":1,\"z\":null},\"modules\":[]", StringComparison.Ordinal),
            canonical.Replace("\"future\":{\"a\":1,\"z\":null}", "\"zFuture\":false,\"aFuture\":true", StringComparison.Ordinal),
            canonical.Replace("\"name\":\"Future\",\"concepts\":[]", "\"concepts\":[],\"name\":\"Future\"", StringComparison.Ordinal),
            canonical.Replace("\"name\":\"Future\"", "\"name\":\"Tampered\"", StringComparison.Ordinal)
        };
        _errors = [.. malformed.Select((json, index) => Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(Encoding.UTF8.GetBytes(index == malformed.Length - 1 ? json : given.documents.SealText(json)), given.documents.Range())))];
    }

    [Fact] void should_refuse_order_encoding_duplicate_name_nfc_and_full_hash_failures() => _errors.All(error => error is InvalidSemanticContract).ShouldBeTrue();
}
#endif
