// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public partial class when_enumeration_numbers_change : given.a_semantic_comparison
{
    const string EnumerationSource = "concept Status : Enum\n  draft = 1\n  issued = 4\n  paid = 7\n  archived = 9\n  cancelled = 11\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        status Status\n";

    void Because() => CompareSnapshots(EnumerationSource, EnumerationSource.Replace("draft = 1", "draft = 2", StringComparison.Ordinal)
        .Replace("issued = 4", "sent = 4", StringComparison.Ordinal).Replace("  paid = 7\n", string.Empty, StringComparison.Ordinal)
        .Replace("  archived = 9\n", string.Empty, StringComparison.Ordinal).Replace("cancelled = 11", "cancelled = 9", StringComparison.Ordinal));

    JsonElement EnumItem(string kind) => Items("members").Single(item => item.GetProperty("changeKind").GetString() == kind && item.GetProperty("member").GetString() != "cancelled");

    [Fact] void should_report_the_baseline_member_name() => EnumItem("enum-number-changed").GetProperty("member").GetString().ShouldEqual("draft");
    [Fact] void should_report_the_surviving_name() => EnumItem("enum-number-changed").GetProperty("afterMember").GetString().ShouldEqual("draft");
    [Fact] void should_report_the_baseline_number() => EnumItem("enum-number-changed").GetProperty("beforeNumber").GetInt32().ShouldEqual(1);
    [Fact] void should_report_the_candidate_number() => EnumItem("enum-member-renamed").GetProperty("afterNumber").GetInt32().ShouldEqual(4);
    [Fact] void should_report_explicit_numbering() => EnumItem("enum-member-renamed").GetProperty("beforeNumbering").GetString().ShouldEqual("explicit");
    [Fact] void should_report_candidate_numbering() => EnumItem("enum-member-renamed").GetProperty("afterNumbering").GetString().ShouldEqual("explicit");
    [Fact] void should_not_mark_explicit_rename_as_breaking() => EnumItem("enum-member-renamed").GetProperty("contractBreaking").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_stored_usage() => EnumItem("enum-member-renamed").GetProperty("stored").GetBoolean().ShouldBeTrue();
    [Fact] void should_emit_null_for_a_removed_members_candidate_number() => EnumItem("enum-member-removed").GetProperty("afterNumber").ValueKind.ShouldEqual(JsonValueKind.Null);
    [Fact] void should_report_reuse_by_a_surviving_name() => EnumItem("enum-number-reused").GetProperty("afterMember").GetString().ShouldEqual("cancelled");
    [Fact] void should_append_fields_only_to_enum_items() => Items("members").Where(item => item.GetProperty("changeKind").GetString() == "changed").All(item => !item.TryGetProperty("beforeNumber", out _)).ShouldBeTrue();
    [Fact] void should_disclose_the_matching_limit() => Diff.GetProperty("limits").EnumerateArray().Any(limit => limit.GetString()!.StartsWith("Enumeration members are matched by name", StringComparison.Ordinal)).ShouldBeTrue();
}
