// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_routed_inventories : given.an_authoring_connection
{
    [Fact]
    void should_separate_construct_readiness_from_whole_workspace_binding_and_catalog_identity()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_renaming_a_source_with_streams.RoutedSource);
        Initialize();
        var opened = Open();
        foreach (var view in new[] { "event-sources", "event-streams", "command-routes" })
        {
            var result = Result("read-workspace", new { expectedRevision = opened.GetProperty("revision").GetString(), view, limit = 20 });
            result.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
            result.GetProperty("executionAvailable").GetBoolean().ShouldBeTrue();
            result.GetProperty("executionReadiness").ValueKind.ShouldEqual(JsonValueKind.Null);
            foreach (var entry in result.GetProperty("page").GetProperty("items").EnumerateArray())
            {
                entry.GetProperty("syntaxOnly").GetBoolean().ShouldBeFalse();
                entry.GetProperty("executionAvailable").GetBoolean().ShouldBeTrue();
                if (view != "command-routes") entry.GetProperty("semanticId").GetString().ShouldNotBeNull();
            }
        }
    }
}
