// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpRevisionDiff;

public class when_comparing_a_path_without_identities_with_a_different_application : given.an_export_with_a_different_application_identity
{
    JsonElement _result;

    void Because() => _result = Compare(new { before = new { path = RootPath }, after = new { workspaceJson = Export } });

    [Fact] void should_allow_different_application_identities() => _result.GetProperty("isError").GetBoolean().ShouldBeFalse();
    [Fact] void should_match_unchanged_declarations_by_address() => _result.GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().ShouldBeEmpty();
    [Fact] void should_report_address_matching() => _result.GetProperty("structuredContent").GetProperty("limits").EnumerateArray().Select(limit => limit.GetString()).ShouldContain("Declarations are matched by exact kind and address because at least one side has no persisted identities; renames and owner moves appear as a removal and an addition.");
    [Fact] void should_add_only_one_matching_limit() => _result.GetProperty("structuredContent").GetProperty("limits").GetArrayLength().ShouldEqual(5);
    [Fact] void should_not_create_identity_state() => Directory.Exists(Path.Combine(RootPath, ".screenplay")).ShouldBeFalse();
}
