// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class a_refactoring_workspace : an_authoring_connection
{
    internal const string RefactoringSource = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed // keep this\n          name String = \"something\"\n    slice StateChange Close\n      command Close\n        projectId Uuid identifier\n        produces event Closed\n          for projectId\n          note String = \"closed\"\n    slice StateChange Archive\n      command Archive\n        projectId Uuid identifier\n        reason String\n        produces ProjectArchived\n          for projectId\n          reason = reason\n      event ProjectArchived\n        reason String\n";

    internal JsonElement OpenedWorkspace;
    internal string CurrentRevision = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), RefactoringSource);
        Initialize();
        OpenedWorkspace = Open();
        CurrentRevision = OpenedWorkspace.GetProperty("revision").GetString()!;
    }

    internal JsonElement[] Offers(string kind) => [.. Page("refactorings", CurrentRevision).EnumerateArray().Where(offer => offer.GetProperty("kind").GetString() == kind)];

    internal string? CatalogRevision => OpenedWorkspace.GetProperty("catalogRevision").GetString();

    internal JsonElement ProposeExtraction(JsonElement subject) => Call("propose-extract-inline-event", new
    {
        expectedRevision = CurrentRevision,
        expectedCatalogRevision = CatalogRevision,
        subject,
        formatting = "CanonicalizeTouchedDocuments"
    });
}
