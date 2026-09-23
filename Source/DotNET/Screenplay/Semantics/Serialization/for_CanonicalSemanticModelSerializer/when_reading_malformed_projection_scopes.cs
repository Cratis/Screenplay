// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

// The reader is strict: every scoped member is required, unknown members and discriminators are rejected, and an explicit
// null scope is not the canonical spelling of a flat projection. Every case must fail in the reader, before revision checks.
public class when_reading_malformed_projection_scopes : Specification
{
    Exception[] _errors;

    void Because()
    {
        var canonical = Encoding.UTF8.GetString(canonical_serialization_golden_vectors.SemanticModelBytes);
        var malformed = new[]
        {
            Once(canonical, "\"removedWithJoin\":", "\"unexpected\":true,\"removedWithJoin\":"),
            Once(canonical, "\"kind\":\"eventSourceIdentity\"", "\"kind\":\"causedBy\""),
            Once(canonical, "\"operation\":\"increment\"", "\"operation\":\"count\""),
            Once(canonical, "\"parentKey\":null,", string.Empty),
            Once(canonical, "\"every\":null", "\"every\":{}"),
            Once(canonical, "\"scope\":{\"from\"", "\"scope\":null,\"ignored\":{\"from\""),
            Once(canonical, "\"kind\":\"composite\",", "\"kind\":\"tuple\","),
            Once(canonical, "\"kind\":\"eventContext\",", "\"kind\":\"eventContext\",\"value\":{\"kind\":\"null\"},")
        };
        _errors = [.. malformed.Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))))];
    }

    [Fact] void should_reject_every_malformed_shape() => _errors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_reject_them_while_reading() => _errors.Any(_ => _.Message.Contains("revision", StringComparison.Ordinal)).ShouldBeFalse();

    static string Once(string json, string find, string replace)
    {
        var index = json.IndexOf(find, StringComparison.Ordinal);
        return index < 0 ? throw new InvalidOperationException($"'{find}' is not in the golden bytes.") : string.Concat(json.AsSpan(0, index), replace, json.AsSpan(index + find.Length));
    }
}
