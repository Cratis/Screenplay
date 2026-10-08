// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSpecificationObligations.given;

public class a_model : for_McpConnection.given.a_connection
{
    internal const string Model = """
        concept ProjectId : Uuid
        concept ProjectName : String
          validate
            not empty message "A name is required"
        policy Staff
          require role "Staff"
        module Projects
          feature Registration
            slice StateChange Register
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                authorize Staff
                validate
                  name min 3 message "Name is too short"
                  require name != "Reserved"
                    message "Name is reserved"
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
              constraint UniqueName
                unique name on ProjectRegistered
                message "Already claimed"
              specification Happy
                given caller
                  role "Staff"
                when RegisterProject
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "First"
                then ProjectRegistered
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  name = "First"
            slice StateView List
              readmodel ProjectList
                projectId ProjectId
                name ProjectName
              projection ProjectList
                from ProjectRegistered
                remove with ProjectRegistered
              query Projects => ProjectList[]
                authorize Staff
            slice Automation FollowUp
              reaction Follow
                when ProjectRegistered
                  produces FollowUpRequested
                    name = name
              event FollowUpRequested
                name ProjectName
        """;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Model);
        Initialize();
    }
}
