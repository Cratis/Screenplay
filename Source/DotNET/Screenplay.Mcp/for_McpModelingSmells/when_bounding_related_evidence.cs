// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_bounding_related_evidence : given.a_model
{
    JsonElement _finding;

    void Establish()
    {
        var copies = string.Join('\n', Enumerable.Range(0, 21).Select(number => $"    slice StateChange Copy{number}\n      command Revise{number}\n        projectId ProjectId identifier\n        name ProjectName\n        produces ProjectCopy{number}\n          for projectId\n          name = name\n      event ProjectCopy{number}\n        name ProjectName"));
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model + "\n" + copies);
    }

    void Because() => _finding = Call("find-modeling-smells", new { scope = "Projects.Maintenance.Register", limit = 1 }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0];

    [Fact] void should_bound_related_declarations() => _finding.GetProperty("related").GetArrayLength().ShouldEqual(20);
    [Fact] void should_keep_the_complete_count() => _finding.GetProperty("relatedCount").GetInt32().ShouldEqual(22);
    [Fact] void should_disclose_omitted_evidence() => _finding.GetProperty("relatedTruncated").GetBoolean().ShouldBeTrue();
}
