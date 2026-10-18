// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

public class a_destination_repair : an_authoring_connection
{
    internal const string DestinationSource = """
        module Projects
          feature Registration
            slice StateChange Register
              command Register
                projectId Uuid identifier
                name String
                // retained exactly once
                produces event Registered
                  name String = name
                produces Audited
                  name = name
                produces when name == "repeat"
                  Audited
                    for projectId
                    name = name
              event Audited
                name String
        """;

    internal McpWorkspaces Workspaces = null!;
    internal JsonElement Opened;
    internal JsonElement Discovery;
    internal string OriginalSource = null!;

    internal void Start(string attachment = "Handler.cs", string content = "opaque implementation", string sourcePrefix = "")
    {
        OriginalSource = sourcePrefix + $"policy Allowed\n  file {attachment}\n" + DestinationSource;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), OriginalSource);
        if (attachment == "Handler.cs") File.WriteAllText(Path.Combine(RootPath, attachment), content);
        Workspaces = new(Root);
        Opened = Content(Workspaces.Open(Arguments(new { applicationName = "Projects" })));
        Discovery = Content(Workspaces.ReadWorkspace(Arguments(new { expectedRevision = Revision, view = "repair-intents", limit = 200 })));
        Discovery.GetProperty("page").GetProperty("items").GetArrayLength().ShouldEqual(3);
    }

    internal string Revision => Opened.GetProperty("revision").GetString()!;
    internal string Catalog => Opened.GetProperty("catalogRevision").GetString()!;

    internal JsonObject DestinationRequest()
    {
        var rows = Discovery.GetProperty("page").GetProperty("items");
        var productions = new JsonArray();
        foreach (var row in rows.EnumerateArray())
        {
            productions.Add(new JsonObject
            {
                ["subject"] = JsonNode.Parse(row.GetProperty("subject").GetRawText()),
                ["for"] = JsonNode.Parse("{\"kind\":\"PathExpressionSyntax\",\"path\":\"projectId\"}"),
                ["route"] = JsonNode.Parse(row.GetProperty("route").GetRawText())
            });
        }

        return new JsonObject
        {
            ["expectedRevision"] = Revision, ["expectedCatalogRevision"] = Catalog,
            ["diagnosticCode"] = "PLAY0470", ["subject"] = JsonNode.Parse(rows[0].GetProperty("command").GetRawText()),
            ["formatting"] = "CanonicalizeTouchedDocuments",
            ["destinationIntent"] = new JsonObject { ["version"] = 1, ["productions"] = productions }
        };
    }

    internal JsonElement ProposeDestination()
    {
        var result = Content(Workspaces.ProposeRepair(Arguments(DestinationRequest())));
        result.GetProperty("success").GetBoolean().ShouldBeTrue();
        return result;
    }

    internal JsonElement Install(JsonElement proposal) => Content(Workspaces.Apply(ApplyArguments(proposal)));
    internal JsonElement ApplyArguments(JsonElement proposal) => Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString(), expectedRevision = Revision, expectedCatalogRevision = Catalog });
    internal JsonElement Review(JsonElement proposal, string view = "production-destinations", int offset = 0, int limit = 1) =>
        Content(Workspaces.ReadProposal(Arguments(new { proposalId = proposal.GetProperty("proposalId").GetString(), view, offset, limit })));

    internal McpAuthoringProposal TypedProposal()
    {
        var workspace = McpAttachmentContents.Refresh(Root, Workspace());
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        rows.Length.ShouldEqual(3);
        var intent = McpDestinationIntent.Read(Arguments(DestinationRequest()).GetProperty("destinationIntent"));
        var result = WorkspaceProductionDestinationRepairs.Propose(workspace, rows[0].Command, intent, new()
        {
            ExpectedRevision = workspace.Revision, ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Executable, Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            AttachmentLoader = documents => McpAttachmentContents.Load(Root, documents)
        });
        result.Accepted.ShouldBeTrue();
        return new(workspace, result, WorkspaceAuthoringValidation.Executable, WorkspaceAuthoringReferencePolicy.Safe);
    }

    internal void ShouldHaveNoInstalledSource()
    {
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(OriginalSource);
        File.Exists(Path.Combine(RootPath, ".screenplay", "identities.json")).ShouldBeFalse();
    }

    internal static void HardLink(string source, string target)
    {
        var start = OperatingSystem.IsWindows()
            ? new ProcessStartInfo("cmd.exe") { ArgumentList = { "/c", "mklink", "/H", target, source }, CreateNoWindow = true }
            : new ProcessStartInfo("/bin/ln") { ArgumentList = { source, target } };
        using var process = Process.Start(start)!;
        process.WaitForExit(5000).ShouldBeTrue();
        process.ExitCode.ShouldEqual(0);
    }

    internal static JsonElement Arguments(object value) => JsonSerializer.SerializeToElement(value, McpJson.Options);
    internal static JsonElement Content(object value) => Arguments(value).GetProperty("structuredContent");
    internal static string FailureOf(Action action) => (Catch.Exception(action) as McpFailure)!.FailureKind;
}
