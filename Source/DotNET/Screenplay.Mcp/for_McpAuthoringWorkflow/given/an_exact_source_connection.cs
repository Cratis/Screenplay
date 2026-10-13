// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class an_exact_source_connection : an_authoring_connection
{
    protected const string Notifications = "// Delivery views\nmodule Notifications\n  feature Delivery\n    slice StateView Inbox";
    protected static readonly string Original = "// Keep this header and its blank line\n\n" + Source
        .Replace("projectId ProjectId", "projectId     ProjectId", StringComparison.Ordinal)
        .Replace("name ProjectName", "name          ProjectName", StringComparison.Ordinal)
        .Replace("name = name", "name      = name", StringComparison.Ordinal);
    protected static readonly string WithImport = Original.Replace("\n\nconcept", "\n\nimport \"Notifications/Notifications.play\"\n\nconcept", StringComparison.Ordinal);
    protected JsonElement Opened;
    protected string ApplicationPath => Path.Combine(RootPath, "application.play");

    void Establish()
    {
        File.WriteAllText(ApplicationPath, Original);
        Initialize();
        Opened = Open();
    }

    protected object ReplaceSource(string source) => new
    {
        operation = "replace-document",
        documentId = Page("documents", Opened.GetProperty("revision").GetString())[0].GetProperty("documentId").GetString(),
        source
    };

    protected Dictionary<string, object?> Arguments(params object[] documents) => new()
    {
        ["expectedRevision"] = Opened.GetProperty("revision").GetString(),
        ["expectedCatalogRevision"] = Opened.GetProperty("catalogRevision").GetString(),
        ["formatting"] = "PreserveExactSource",
        ["documents"] = documents
    };

    protected object CreateNotifications() => new { operation = "create-document", path = "Notifications/Notifications.play", stableKey = "notifications", source = Notifications };
}
