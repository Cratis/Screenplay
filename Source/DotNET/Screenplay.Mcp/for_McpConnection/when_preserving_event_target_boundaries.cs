// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Indexing.for_AuthoringIndex.given;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_preserving_event_target_boundaries : given.a_connection
{
    (string Name, JsonElement Result, string Target, string[] Candidates)[] _cases = [];
    readonly List<JsonElement> _admissionResults = [];

    void Establish() => Initialize();

    void Because()
    {
        var cases = new List<(string, JsonElement, string, string[])>();
        string[] previous = [];
        foreach (var value in event_target_applications.Boundaries())
        {
            foreach (var path in previous) File.Delete(Path.Combine(RootPath, path));
            foreach (var source in value.Sources) File.WriteAllText(Path.Combine(RootPath, source.Key), source.Value);
            previous = [.. value.Sources.Keys];
            var result = Call("dependencies", new { address = "M.F.Publish.P", kind = value.Name == "event reducer" ? "Reducer" : "Projection", direction = "outgoing", descendants = true }).GetProperty("result").GetProperty("structuredContent");
            cases.Add((value.Name, result, value.Target, value.Candidates));
            if (value.Name == "same slice readmodel" || value.Name == "projection name fallback")
            {
                _admissionResults.Add(Call("find-references", new { address = "M.F.Publish." + value.Target, kind = "Event" }).GetProperty("result").GetProperty("structuredContent"));
            }
        }
        _cases = [.. cases];
    }

    [Fact]
    void should_keep_admission_failures_and_their_authored_diagnostics()
    {
        _admissionResults.Count.ShouldEqual(2);
        foreach (var result in _admissionResults)
        {
            result.GetProperty("success").GetBoolean().ShouldBeFalse();
            result.GetProperty("diagnostics").EnumerateArray().Where(diagnostic => diagnostic.GetProperty("code").GetString() == "PLAY0609").Select(diagnostic => diagnostic.GetProperty("message").GetString()).ShouldEqual(["Outbound Translate slice 'Publish' must produce exactly one local public event type; found 0."]);
        }
    }

    [Fact]
    void should_keep_requested_kinds_resolution_and_candidate_order()
    {
        foreach (var value in _cases)
        {
            var edge = value.Result.GetProperty("page").GetProperty("items").EnumerateArray().Single(edge => edge.GetProperty("reference").GetProperty("role").GetString() == "builds" || edge.GetProperty("reference").GetProperty("role").GetString() == "buildsVariant");
            edge.GetProperty("reference").GetProperty("name").GetString().ShouldEqual(value.Target);
            edge.GetProperty("reference").GetProperty("kinds").EnumerateArray().Select(kind => kind.GetString()).ShouldEqual(["ReadModel"]);
            edge.GetProperty("resolution").GetString().ShouldEqual(value.Candidates.Length == 0 ? "unresolved" : "resolved");
            edge.GetProperty("targets").EnumerateArray().Select(target => target.GetProperty("address").GetString()).ShouldEqual(value.Candidates);
        }
    }
}
