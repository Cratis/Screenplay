// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations;

public class when_commands_drive_removal_and_reactions : given.a_model
{
    JsonElement[] _items = [];

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model + "\n" + """
              reaction OnStartup
                when Startup
                  produces FollowUpRequested
                    for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                    name = "Startup"
              specification Starting
                when trigger Startup
                then FollowUpRequested
                  name = "Startup"
              specification Registering
                when RegisterProject
                  name = "First"
                then no readmodel ProjectList for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then FollowUpRequested
                  name = "First"
        """);
    }

    void Because() => _items = [.. Call("find-specification-obligations").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_match_command_driven_removal() => _items.Single(item => item.GetProperty("ruleId").GetString() == "SPEC007").GetProperty("status").GetString().ShouldEqual("met");
    [Fact] void should_match_command_driven_and_builtin_reactions() => _items.Where(item => item.GetProperty("ruleId").GetString() == "SPEC008").Select(item => item.GetProperty("status").GetString()).ShouldContainOnly("met", "met");
}
