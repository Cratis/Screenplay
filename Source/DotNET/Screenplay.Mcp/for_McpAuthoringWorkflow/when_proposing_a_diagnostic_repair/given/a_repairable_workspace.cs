// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_proposing_a_diagnostic_repair.given;

public class a_repairable_workspace : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    protected JsonElement Opened;
    protected JsonElement Repair;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source.Replace("        produces ProjectRegistered", "        validate csharp\n          ```\n          yield return \"invalid\";\n          ```\n        produces ProjectRegistered", StringComparison.Ordinal));
        Initialize();
        Opened = Open();
        Repair = Result("read-workspace", new { expectedRevision = Opened.GetProperty("revision").GetString(), view = "repairs" })
            .GetProperty("page").GetProperty("items").EnumerateArray().Single();
    }

    protected object Arguments(string? revision = null, object? subject = null) => new
    {
        expectedRevision = revision ?? Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        diagnosticCode = Repair.GetProperty("diagnosticCode").GetString(),
        subject = subject ?? Repair.GetProperty("subject"),
        formatting = "CanonicalizeTouchedDocuments"
    };
}
