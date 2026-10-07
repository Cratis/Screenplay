// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_an_assigned_declaration_is_not_comparable : given.a_semantic_comparison
{
    void Because()
    {
        const string skeleton = "concept Retained : String\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n";
        const string operation = "      operation Save\n        uses Store\n";
        const string before = "system Store\n" + skeleton + operation;
        CompareSnapshots(before, before.Replace("concept Retained : String\n", string.Empty, StringComparison.Ordinal), seed: Create(skeleton));
    }

    [Fact] void should_retain_the_uncomparable_catalog_assignment() => Proposal.Workspace.IdentityCatalog.Semantics.Any(assignment => assignment.Address.Kind == SemanticKind.Concept && assignment.Address.Name == "Retained").ShouldBeTrue();
    [Fact] void should_count_non_comparable_assignments_of_every_kind() => Section("members").GetProperty("unavailable").EnumerateArray().Any(reason => reason.GetString().Contains("assigned semantic IDs", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_mark_the_member_section_incomplete() => Section("members").GetProperty("complete").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_report_no_semantic_change() => Diff.GetProperty("hasSemanticChange").ValueKind.ShouldEqual(JsonValueKind.Null);
}
