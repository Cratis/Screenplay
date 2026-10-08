// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_an_unassigned_application_member_changes : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "domain Before\nsystem Store\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      operation Save\n        uses Store\n";
        CompareSnapshots(source, source.Replace("domain Before", "domain After", StringComparison.Ordinal));
    }

    [Fact] void should_compare_available_root_members_without_a_catalog_id() => Items("members").Single(item => item.GetProperty("kind").GetString() == "Application").GetProperty("semanticId").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_the_known_domain_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
    [Fact] void should_keep_identity_coverage_incomplete() => Section("identities").GetProperty("complete").GetBoolean().ShouldBeFalse();
}
