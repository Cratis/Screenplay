// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_source_only_revisions : given.two_snapshots
{
    JsonElement _result;

    void Because()
    {
        var workspace = Create("system Store\nmodule Projects\n  feature Registration\n    slice StateView List\n      operation LoadProjects\n        uses Store\n        projectId Uuid\n      readmodel Projects\n        name String\n      query All => Projects[]\n      screen List\n        data Projects via query All\n");
        var snapshot = Export(workspace);
        _result = Call(new { beforeWorkspaceJson = snapshot, afterWorkspaceJson = snapshot }).GetProperty("structuredContent");
    }

    [Fact] void should_compare_without_executable_binding() => _result.GetProperty("executableAfterAvailable").GetBoolean().ShouldBeFalse();
    [Fact] void should_disclose_incomplete_sections() => _result.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_claim_no_change_from_missing_information() => _result.GetProperty("hasSemanticChange").ValueKind.ShouldEqual(JsonValueKind.Null);
}
