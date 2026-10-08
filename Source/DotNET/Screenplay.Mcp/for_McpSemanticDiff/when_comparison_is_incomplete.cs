// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_comparison_is_incomplete : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateView List\n      readmodel Projects\n        name String\n      query All => Projects[]\n      screen List\n        data Projects via query All\n";
        var fullSource = "system Store\n" + source.Replace("      readmodel Projects", "      operation LoadProjects\n        uses Store\n        projectId Uuid\n      readmodel Projects", StringComparison.Ordinal);
        Workspace = Create(fullSource);
        Propose(fullSource);
        Diff = Read();
    }

    [Fact] void should_disclose_the_unassigned_declaration_section() => Diff.GetProperty("sections").EnumerateArray().Single(section => section.GetProperty("section").GetString() == "declarations").GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_claim_the_whole_comparison_is_complete() => Diff.GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_unknown_instead_of_no_semantic_change() => Diff.GetProperty("hasSemanticChange").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_work_without_executable_binding() => Diff.GetProperty("executableAfterAvailable").GetBoolean().ShouldBeFalse();
}
