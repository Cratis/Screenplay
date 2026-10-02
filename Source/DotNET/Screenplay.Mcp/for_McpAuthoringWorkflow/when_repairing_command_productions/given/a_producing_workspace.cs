// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_repairing_command_productions.given;

public class a_producing_workspace : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    protected JsonElement Opened;
    protected JsonElement Repair;
    protected JsonElement Proposal;
    protected byte[] Original;

    protected void OpenProducingWorkspace(bool declared, string code)
    {
        var source = """
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    projectId Uuid identifier
                    name String
                    produces ProjectRegistered // preserve this comment
                      name = name
                      registeredAt = $context.occurred
            """;
        if (declared)
        {
            source += "\n      event ProjectRegistered\n        name String\n        registeredAt DateTime\n";
        }

        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Original = File.ReadAllBytes(Path.Combine(RootPath, "application.play"));
        Initialize();
        Opened = Open();
        Repair = Page("repairs", Opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Single(repair => repair.GetProperty("diagnosticCode").GetString() == code);
    }

    protected object Arguments(string? revision = null) => new
    {
        expectedRevision = revision ?? Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        diagnosticCode = Repair.GetProperty("diagnosticCode").GetString(),
        subject = Repair.GetProperty("subject"),
        formatting = "CanonicalizeTouchedDocuments"
    };
}
