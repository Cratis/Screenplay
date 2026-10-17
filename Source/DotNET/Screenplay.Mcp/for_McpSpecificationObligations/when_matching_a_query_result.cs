// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_matching_a_query_result : given.a_query_model
{
    JsonElement[] _items = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model + "\n" + """
              specification ReadingOwnNotes
                given caller
                  authenticated
                  claim "memberId" = "m1"
                given readmodel NoteRow
                  ownerId = "m1"
                  text = "A note"
                then query NotesOfOwner
                  arguments
                    ownerId = "m1"
                  result
                    ownerId = "m1"
                    text = "A note"
        """);

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_meet_the_view_assertion_obligation() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC006").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_link_the_result_specification() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC006").GetProperty("specifications")[0].GetProperty("name").GetString().ShouldEqual("ReadingOwnNotes");
    [Fact] void should_not_count_success_as_denial() => _items.Where(item => item.GetProperty("ruleId").GetString() == "SPEC004").Select(item => item.GetProperty("status").GetString()).ShouldContainOnly("unmet", "unmet");
}
