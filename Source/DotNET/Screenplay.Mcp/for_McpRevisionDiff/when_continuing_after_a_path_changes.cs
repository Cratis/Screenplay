// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_continuing_after_a_path_changes : given.comparison_sources
{
    JsonElement _result;
    string _revision = null!;

    void Establish()
    {
        var first = Compare(new { before = new { path = "." }, after = new { workspaceJson = Export }, limit = 1 });
        _revision = first.GetProperty("structuredContent").GetProperty("sourceRevision").GetString()!;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("Registers a new project", "Registers a project", StringComparison.Ordinal));
    }

    void Because() => _result = Compare(new { before = new { path = "." }, after = new { workspaceJson = Export }, offset = 1, limit = 1, expectedSourceRevision = _revision });

    [Fact] void should_refuse_the_old_pair_revision() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_a_stale_continuation() => _result.GetProperty("structuredContent").GetProperty("failureKind").GetString().ShouldEqual("StaleRevision");
}
