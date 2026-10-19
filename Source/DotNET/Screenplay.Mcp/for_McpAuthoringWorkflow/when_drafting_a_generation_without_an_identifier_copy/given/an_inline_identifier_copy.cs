// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_drafting_a_generation_without_an_identifier_copy.given;

public class an_inline_identifier_copy : for_McpAuthoringWorkflow.given.an_authoring_connection
{
    internal const string InlineSource = """
        module Projects
          feature Naming
            slice StateChange Rename
              command Rename
                projectId Uuid identifier
                produces event Renamed
                  projectId Uuid = projectId
                  name String = "something"
        """;

    protected JsonElement Opened;
    protected JsonElement Copy;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), InlineSource, new UTF8Encoding(false));
        Initialize();
        Opened = Open();
        Copy = Result("read-ast", new { expectedRevision = Opened.GetProperty("revision").GetString(), kind = "PropertySyntax", name = "projectId" }).GetProperty("page").GetProperty("items").EnumerateArray()
            .Single(node => node.GetProperty("address").GetProperty("parts").EnumerateArray().Any(part => part.GetProperty("kind").GetString() == "Declaration" && part.GetProperty("key").GetString() == "Renamed"));
    }

    protected object Arguments() => new
    {
        expectedRevision = Opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
        action = "remove",
        target = Copy.GetProperty("handle"),
        expectedName = "projectId",
        newGeneration = true,
        formatting = "CanonicalizeTouchedDocuments"
    };
}
