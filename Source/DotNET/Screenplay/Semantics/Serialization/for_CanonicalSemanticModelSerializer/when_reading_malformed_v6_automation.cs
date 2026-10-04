// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_reading_malformed_v6_automation : Specification
{
    Exception[] _errors;

    void Because()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelV6Bytes);
        var malformed = new[]
        {
            Once(canonical, "\"invokes\":", "\"unknownEffect\":true,\"invokes\":"),
            Once(canonical, "\"kind\":\"interval\"", "\"kind\":\"interval\",\"kind\":\"interval\""),
            Once(canonical, "\"givenClock\":", "\"givenClock\":\"2026-10-02T09:00:00.0000000Z\",\"givenClock\":"),
            Once(canonical, "\"kind\":\"schedule\"", "\"kind\":\"unknownSchedule\""),
            Once(canonical, "\"identifiedBy\":", "\"unexpectedIdentity\":"),
            Once(canonical, "\"schemaVersion\":6", "\"schemaVersion\":5")
        };
        _errors = [.. malformed.Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))))];
    }

    [Fact] void should_reject_unknown_duplicate_and_version_inappropriate_fields() => _errors.All(error => error is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_not_hide_malformed_fields_as_a_revision_mismatch() => _errors.Any(error => error.Message.Contains("revision", StringComparison.Ordinal)).ShouldBeFalse();

    static string Once(string json, string find, string replace)
    {
        var index = json.IndexOf(find, StringComparison.Ordinal);
        index.ShouldBeGreaterThan(-1);
        return string.Concat(json.AsSpan(0, index), replace, json.AsSpan(index + find.Length));
    }
}
