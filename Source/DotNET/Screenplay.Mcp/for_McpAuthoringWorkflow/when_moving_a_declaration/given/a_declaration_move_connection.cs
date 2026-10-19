// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_moving_a_declaration.given;

public class a_declaration_move_connection : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    internal const string Model = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Source
            slice StateChange Registration
              command Register
                projectId ProjectId identifier
                name      ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
              specification Registering
                when Register
                  projectId = "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name      = "A"
                then ProjectRegistered
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "A"
            slice StateView Listing
              readmodel ProjectView
                name ProjectName
              query ProjectNamed => ProjectView optional
                by name ProjectName
              projection Listing => ProjectView
                from ProjectRegistered
                  name = name
              specification FindingAProject
                given ProjectRegistered
                  for "0f8fad5b-d9cb-469f-a165-70867728950e"
                  name = "A"
                when query ProjectNamed
                  name = "A"
                then result
                  name = "A"
          feature Destination
            slice StateChange Intake
            slice StateView Lookup
        """;

    internal JsonElement Opened;
    internal string Revision = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
        Opened = Open();
        Revision = Opened.GetProperty("revision").GetString()!;
    }

    internal JsonElement Address(string kind, string name) => Page("semantics", Revision).EnumerateArray()
        .Single(item => item.GetProperty("address").GetProperty("kind").GetString() == kind && item.GetProperty("address").GetProperty("parts").EnumerateArray().Last().GetProperty("key").GetString() == name)
        .GetProperty("address");

    internal JsonElement ProposeMove(JsonElement target, JsonElement newParent) => Call("propose-move", new
    {
        expectedRevision = Revision,
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        target,
        newParent,
        formatting = "CanonicalizeTouchedDocuments"
    }).GetProperty("result");
}
