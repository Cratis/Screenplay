// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpModelingSmells.given;

public class a_model : for_McpConnection.given.a_connection
{
    internal const string Model = """
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          feature Maintenance
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  name = name
              event ProjectRegistered
                name ProjectName
            slice StateChange Revise
              command UpdateProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectUpdated
                  for projectId
                  name = name
                produces RevisionApproved
                  for projectId
                  approved = true
              event ProjectUpdated
                name ProjectName
              event RevisionApproved
                approved Bool
            slice StateView List
              readmodel ProjectList
                name ProjectName
              projection ProjectList
                from ProjectRegistered
                from ProjectUpdated
        """;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
    }
}
