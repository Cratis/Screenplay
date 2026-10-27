// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_refusing_hostile_wire_versions : Specification
{
    Exception?[] _errors;

    void Because()
    {
        JsonNode?[] values = [null, JsonValue.Create(1), JsonValue.Create(true), new JsonArray(), new JsonObject(), JsonValue.Create(string.Empty), JsonValue.Create("1"), JsonValue.Create("1."), JsonValue.Create("1.4294967296")];
        _errors = [.. new[] { "languageVersion", "semanticVersion" }.SelectMany(property => values.Select(value => Refusal(property, value)).Append(Missing(property)))];
    }

    [Fact] void should_refuse_every_null_non_string_malformed_and_missing_version_as_invalid_contract() => _errors.All(error => error is InvalidSemanticContract).ShouldBeTrue();

    static Exception? Refusal(string property, JsonNode? value)
    {
        var root = given.documents.Minimal();
        root[property] = value?.DeepClone();
        return Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));
    }

    static Exception? Missing(string property)
    {
        var root = given.documents.Minimal();
        root.Remove(property);
        return Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));
    }
}
#endif
