// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_a_path_with_an_export : given.comparison_sources
{
    JsonElement _result;

    void Because() => _result = Compare(new { before = new { path = "." }, after = new { workspaceJson = Export } });

    [Fact] void should_admit_both_source_forms() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_compare_the_same_revision() => _result.GetProperty("structuredContent").GetProperty("beforeRevision").GetString().ShouldEqual(_result.GetProperty("structuredContent").GetProperty("afterRevision").GetString());
    [Fact] void should_find_no_changes() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    [Fact] void should_leave_the_folder_read_only() => Directory.GetFileSystemEntries(RootPath).Select(Path.GetFileName).ShouldContainOnly("application.play");
}
