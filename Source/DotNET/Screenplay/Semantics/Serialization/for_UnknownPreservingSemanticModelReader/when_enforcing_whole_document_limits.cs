// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json.Nodes;

namespace Cratis.Screenplay.Semantics.Serialization.for_UnknownPreservingSemanticModelReader;

public class when_enforcing_whole_document_limits : Specification
{
    Exception?[] _boundaries;
    Exception?[] _breaches;

    void Because()
    {
        _boundaries = [Read(Depth(62)), Read(Unknowns(64)), Read(Owners(1024)), Read(ValueBytes(1_048_576)), Read(Name(64))];
        _breaches = [Read(Depth(63)), Read(Unknowns(65)), Read(Owners(1025)), Read(ValueBytes(1_048_577)), Read(Name(65))];
    }

    [Fact] void should_accept_each_inclusive_boundary() => _boundaries.Select((error, index) => error is null ? null : $"Boundary {index}: {error}").Where(error => error is not null).ShouldBeEmpty();
    [Fact] void should_refuse_the_whole_document_after_each_boundary() => _breaches.Select((error, index) => error is InvalidSemanticContract ? null : $"Breach {index}: {error}").Where(error => error is not null).ShouldBeEmpty();

    static Exception? Read(JsonObject root) => Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(given.documents.Seal(root), given.documents.Range()));

    static JsonObject Depth(int depth)
    {
        var root = given.documents.Minimal();
        JsonNode? nested = null;
        for (var index = 0; index < depth; index++) nested = new JsonArray(nested);
        root["application"]!["future"] = nested;
        return root;
    }

    static JsonObject Unknowns(int count)
    {
        var root = given.documents.Minimal();
        AddMembers(root["application"]!.AsObject(), count);
        return root;
    }

    static JsonObject Owners(int total)
    {
        var root = given.documents.Minimal();
        var concepts = root["application"]!["concepts"]!.AsArray();
        for (var owner = 0; total > 0; owner++)
        {
            var concept = new JsonObject
            {
                ["id"] = given.documents.Id(owner + 2), ["name"] = $"Concept{owner}", ["primitive"] = "string", ["values"] = new JsonArray(), ["validations"] = new JsonArray()
            };
            var count = Math.Min(total, 64);
            AddMembers(concept, count);
            concepts.Add(concept);
            total -= count;
        }

        return root;
    }

    static void AddMembers(JsonObject owner, int count)
    {
        for (var index = 0; index < count; index++) owner[$"x{index:D3}"] = true;
    }

    static JsonObject ValueBytes(int bytes)
    {
        var root = given.documents.Minimal();
        root["application"]!["future"] = new string('x', bytes - 2);
        return root;
    }

    static JsonObject Name(int length)
    {
        var root = given.documents.Minimal();
        root["application"]![new string('x', length)] = true;
        return root;
    }
}
#endif
