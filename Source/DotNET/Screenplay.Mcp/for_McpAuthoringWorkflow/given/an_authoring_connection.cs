// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class an_authoring_connection : for_McpConnection.given.a_connection
{
    internal const string FullSource = """
        import External.Customer
        concept ProjectId : Uuid
        concept ProjectName : String
        module Projects
          screen template Shell
            navbar contributes Navigation
            main
          contribute to Navigation
            navigate to ProjectList
          form RegisterProjectForm for RegisterProject
            field name label "Project name"
            on submit navigate to ProjectList
          feature Registration
            slice StateChange RegisterProject
              description "Registers café projects 🚀"
              command RegisterProject
                projectId ProjectId identifier
                name ProjectName
                produces ProjectRegistered
                  for projectId
                  projectId = projectId
                  name = name
              event ProjectRegistered
                projectId ProjectId
                name ProjectName
            slice StateView List
              readmodel Projects
                name ProjectName
              query GetProjects => Projects[]
              screen ProjectList
                data Projects via query GetProjects
          feature Archive
            feature History
              slice StateView Timeline
        module Administration
          feature Settings
            slice StateView Preferences
        """;

    internal JsonElement Result(string tool, object? arguments = null)
    {
        var response = Call(tool, arguments);
        if (response.TryGetProperty("error", out var error))
        {
            throw new McpFailure($"{tool}: {error.GetRawText()}");
        }

        var result = response.GetProperty("result");
        if (result.GetProperty("isError").GetBoolean())
        {
            throw new McpFailure($"{tool}: {result.GetRawText()}");
        }

        return result.GetProperty("structuredContent");
    }

    internal JsonElement Open() => Result("open-workspace", new { applicationName = "Projects" });

    internal JsonElement Page(string view, string revision) => Result("read-workspace", new { expectedRevision = revision, view, limit = 200 }).GetProperty("page").GetProperty("items");

    internal JsonElement Node(string kind, string revision) => Result("read-ast", new { expectedRevision = revision, kind, includeContent = true }).GetProperty("page").GetProperty("items").EnumerateArray().Single();

    internal JsonElement Apply(JsonElement opened, JsonElement proposal) => Result("apply", new
    {
        expectedRevision = opened.GetProperty("revision").GetString(),
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        proposalId = proposal.GetProperty("proposalId").GetString()
    });

    internal byte[] Export(string revision, string proposalId)
    {
        var bytes = new List<byte>();
        var offset = 0;
        while (true)
        {
            var page = Result("export-workspace", new { expectedRevision = revision, proposalId, offset, limit = 257 });
            page.GetProperty("offset").GetInt32().ShouldEqual(offset);
            bytes.AddRange(page.GetProperty("bytesBase64").GetBytesFromBase64());
            if (page.GetProperty("nextOffset").ValueKind == JsonValueKind.Null)
            {
                bytes.Count.ShouldEqual(page.GetProperty("totalBytes").GetInt32());
                break;
            }

            offset = page.GetProperty("nextOffset").GetInt32();
        }

        return [.. bytes];
    }

    internal ScreenplayWorkspace Candidate(JsonElement proposal) => ScreenplayWorkspaceSerializer.Deserialize(Export(
        proposal.GetProperty("after").GetProperty("revision").GetString(), proposal.GetProperty("proposalId").GetString()));
}
