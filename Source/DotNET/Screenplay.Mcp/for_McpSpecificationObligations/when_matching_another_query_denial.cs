// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_matching_another_query_denial : given.a_query_model
{
    JsonElement[] _items = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model + "\n" + """
              specification DenyingOtherNotes
                given caller
                  authenticated
                  claim "memberId" = "m2"
                then query OtherNotes
                  arguments
                    ownerId = "m1"
                then denied
        """);

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_not_meet_the_unrelated_query_denial_obligation() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC004" && item.GetProperty("declaration").GetProperty("name").GetString() == "NotesOfOwner").GetProperty("status").GetString().ShouldEqual("unmet");
    [Fact] void should_not_link_unrelated_evidence() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC004" && item.GetProperty("declaration").GetProperty("name").GetString() == "NotesOfOwner").GetProperty("specificationCount").GetInt32().ShouldEqual(0);
    [Fact] void should_meet_only_the_queried_denial_obligation() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC004" && item.GetProperty("declaration").GetProperty("name").GetString() == "OtherNotes").GetProperty("status").GetString().ShouldEqual("met");
}
