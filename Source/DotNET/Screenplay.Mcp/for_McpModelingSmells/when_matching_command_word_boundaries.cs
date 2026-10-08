// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells;

public class when_matching_command_word_boundaries : given.a_model
{
    JsonElement[] _items = [];

    void Establish() => File.WriteAllText(Path.Combine(RootPath, "application.play"), Model.Replace("UpdateProject", "SettleInvoice", StringComparison.Ordinal));

    void Because() => _items = [.. Call("find-modeling-smells").GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("items").EnumerateArray()];

    [Fact] void should_not_treat_settle_as_the_generic_set_verb() => _items.Any(item => item.GetProperty("ruleId").GetString() == "SMELL002").ShouldBeFalse();
}
