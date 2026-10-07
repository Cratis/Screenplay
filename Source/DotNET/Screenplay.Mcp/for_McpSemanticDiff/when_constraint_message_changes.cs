// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_constraint_message_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = """
            module Projects
              feature Registration
                slice StateChange Register
                  event Registered
                    name String
                  constraint UniqueName
                    unique name on Registered
                    message "Before"
            """;
        CompareSnapshots(source, source.Replace("\"Before\"", "\"After\"", StringComparison.Ordinal));
    }

    [Fact] void should_report_the_constraint_message_change() => Items("members").Single(item => item.GetProperty("kind").GetString() == "Constraint").GetProperty("member").GetString().ShouldEqual("message");
    [Fact] void should_not_fabricate_a_constraint_identity() => Items("members").Single(item => item.GetProperty("kind").GetString() == "Constraint").GetProperty("semanticId").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_identity_comparison_as_incomplete() => Section("members").GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_known_change_despite_incomplete_identity_coverage() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
