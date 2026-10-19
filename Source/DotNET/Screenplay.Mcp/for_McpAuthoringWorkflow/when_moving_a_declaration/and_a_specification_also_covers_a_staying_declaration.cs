// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_moving_a_declaration;

public class and_a_specification_also_covers_a_staying_declaration : given.a_declaration_move_connection
{
    JsonElement _result;

    void Because() => _result = ProposeMove(Address("Query", "ProjectNamed"), Address("Slice", "Lookup"));

    string Message => _result.GetProperty("structuredContent").GetProperty("conflicts")[0].GetProperty("message").GetString()!;

    [Fact] void should_refuse() => _result.GetProperty("isError").GetBoolean().ShouldBeTrue();
    [Fact] void should_name_the_specification() => Message.ShouldContain("Projects.Source.Listing.FindingAProject");
    [Fact] void should_name_the_staying_declaration() => Message.ShouldContain("ReadModel 'Projects.Source.Listing.ProjectView'");
}
