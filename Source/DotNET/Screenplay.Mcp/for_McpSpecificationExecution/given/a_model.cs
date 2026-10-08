// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSpecificationExecution.given;

public class a_model : for_McpConnection.given.a_connection
{
    internal const string Scenarios = """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId ProjectId identifier
                name String
                produces Registered
                  for projectId
                  projectId = projectId
                  name = name
              event Registered
                projectId ProjectId
                name String
              specification Correct
                when Register
                  projectId = "00000000-0000-0000-0000-000000000101"
                  name = "First"
                then Registered
                  projectId = "00000000-0000-0000-0000-000000000101"
                  name = "First"
              specification Wrong
                when Register
                  projectId = "00000000-0000-0000-0000-000000000101"
                  name = "First"
                then Registered
                  projectId = "00000000-0000-0000-0000-000000000101"
                  name = "First"
                then readmodel Summary
                  projectId = "00000000-0000-0000-0000-000000000101"
                  name = "Other"
            slice StateView Lookup
              readmodel Summary
                projectId ProjectId
                name String
              query ById => Summary optional
                by projectId ProjectId
              projection SummaryProjection => Summary
                from Registered
        """;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Scenarios);
        Initialize();
    }
}
