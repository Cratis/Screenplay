// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_matching_an_explicit_query_denial : given.a_query_model
{
    JsonElement[] _items = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model + "\n" + """
              specification DenyingAnotherMember
                given caller
                  authenticated
                  claim "memberId" = "m2"
                when query NotesOfOwner
                  ownerId = "m1"
                then denied
        """);

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_meet_the_query_denial_obligation() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC004" && item.GetProperty("declaration").GetProperty("name").GetString() == "NotesOfOwner").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_link_the_denial_specification() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC004" && item.GetProperty("declaration").GetProperty("name").GetString() == "NotesOfOwner").GetProperty("specifications")[0].GetProperty("name").GetString().ShouldEqual("DenyingAnotherMember");
    [Fact] void should_not_count_denial_as_a_view_assertion() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC006").GetProperty("specificationCount").GetInt32().ShouldEqual(0);
}
