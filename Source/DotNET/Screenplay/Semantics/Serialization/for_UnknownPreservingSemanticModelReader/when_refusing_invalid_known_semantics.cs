// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_refusing_invalid_known_semantics : Specification
{
    Exception?[] _errors;

    void Because()
    {
        var unknownKind = given.documents.Future();
        given.documents.Find(unknownKind, "type")["kind"] = "future";
        var dangling = given.documents.Future();
        var type = given.documents.Find(dangling, "type");
        type["kind"] = "concept";
        type.Remove("primitive");
        type["target"] = given.documents.Id(987654);
        var forbidden = given.documents.Future();
        forbidden["application"]!["concepts"]![0]!["generation"] = JsonNode.Parse("{\"kind\":\"uuid\"}");
        var unsorted = given.documents.Future();
        var concepts = unsorted["application"]!["concepts"]!.AsArray();
        unsorted["application"]!["concepts"] = new JsonArray([.. concepts.Reverse().Select(concept => concept!.DeepClone())]);
        _errors = [.. new[] { unknownKind, dangling, forbidden, unsorted }.Select(root => Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range())))];
    }

    [Fact] void should_not_hide_unknown_variants_dangling_references_or_version_forbidden_members() => _errors.All(error => error is InvalidSemanticContract).ShouldBeTrue();
}
#endif
