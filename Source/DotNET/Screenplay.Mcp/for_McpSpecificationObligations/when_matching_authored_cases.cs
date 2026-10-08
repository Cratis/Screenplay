// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_matching_authored_cases : given.a_model
{
    JsonElement[] _items = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model.Replace(
        "    slice StateView List",
        """
              specification Reserved
                when RegisterProject
                  name = "Reserved"
                then error "Name is reserved"
              specification Short
                when RegisterProject
                  name = "A"
                then error "Name is too short"
              specification Empty
                when RegisterProject
                  name = ""
                then error "A name is required"
              specification Denial
                when RegisterProject
                then denied
              specification Competing
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "First"
                when RegisterProject
                  projectId = "00000000-0000-0000-0000-000000000001"
                  name = "First"
                then error "Already claimed"
            slice StateView List
        """,
        StringComparison.Ordinal).Replace(
        "    slice Automation FollowUp",
        """
              specification Denial
                when query Projects
                then denied
              specification Removed
                when append ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "First"
                then no readmodel ProjectList for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
            slice Automation FollowUp
        """,
        StringComparison.Ordinal) + "\n" + """
              specification Reaction
                when append ProjectRegistered
                  name = "First"
                then FollowUpRequested
                  name = "First"
        """);

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_find_all_matching_specifications() => _items.Where(item => item.GetProperty("status").GetString() != "met").Select(item => item.ToString()).ShouldBeEmpty();
    [Fact] void should_keep_individual_rejection_evidence() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC002").GetProperty("specifications")[0].GetProperty("name").GetString().ShouldEqual("Reserved");
}
