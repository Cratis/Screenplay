// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_the_event_is_a_public_contract : given.a_model
{
    JsonElement[] _unread = [];

    void Because() => _unread = Unread(Model.Replace("      event RevisionApproved", "      public event RevisionApproved", StringComparison.Ordinal));

    [Fact] void should_not_flag_its_properties() => _unread.ShouldBeEmpty();

    JsonElement[] Unread(string source)
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);

        return [.. Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray().Where(item => item.GetProperty("ruleId").GetString() == "SMELL006")];
    }
}
