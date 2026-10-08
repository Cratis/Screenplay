// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_capture_key_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = """
            module Projects
              feature Registration
                slice Translate Import
                  capture Sync
                    source api
                      api LegacyApi
                    key id
                    append Imported
                      when status
                        status = $.status
                  event Imported
                    status String
            """;
        CompareSnapshots(source, source.Replace("key id", "key invoiceId", StringComparison.Ordinal));
    }

    [Fact] void should_have_an_assigned_capture_to_compare() => Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Kind == SemanticKind.Capture).ShouldBeTrue();
    [Fact] void should_report_the_assigned_capture_key_change() => Items("members").Any(item => item.GetProperty("kind").GetString() == "Capture" && item.GetProperty("member").GetString() == "key" && item.GetProperty("semanticId").GetString() is not null).ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
