// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_moving_a_declaration;

public class and_its_specification_covers_only_it : given.a_declaration_move_connection
{
    JsonElement _result;

    void Because() => _result = ProposeMove(Address("Command", "Register"), Address("Slice", "Intake"));

    JsonElement Report => _result.GetProperty("structuredContent").GetProperty("moveReport");

    [Fact] void should_accept() => Assert.False(_result.GetProperty("isError").GetBoolean(), _result.GetRawText());
    [Fact] void should_carry_the_specification() => Report.GetProperty("specificationsCarried").EnumerateArray().Select(address => address.GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString()).ShouldContainOnly("Registering");
    [Fact] void should_carry_it_to_the_destination_slice() => Report.GetProperty("specificationsCarried")[0].GetProperty("parts").EnumerateArray().Select(part => part.GetProperty("key").GetString()).ShouldContain("Intake");
    [Fact] void should_retire_nothing() => Report.GetProperty("retired").GetArrayLength().ShouldEqual(0);
}
