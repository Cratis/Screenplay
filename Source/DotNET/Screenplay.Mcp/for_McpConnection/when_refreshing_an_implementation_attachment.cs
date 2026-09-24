// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax.Serialization;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpConnection;

public class when_refreshing_an_implementation_attachment : given.a_connection
{
    string _first = null!;
    string _second = null!;
    string _revision = null!;

    void Because()
    {
        var sourcePath = Path.Combine(RootPath, "application.play");
        File.WriteAllText(sourcePath, "module Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        handler\n          file Handler.cs");
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "first");
        Initialize();
        var response = Call("open-workspace");
        var opened = response.GetProperty("result").GetProperty("structuredContent");
        _revision = opened.TryGetProperty("revision", out var revision) ? revision.GetString()! : throw new InvalidOperationException(opened.ToString());
        _first = Hash();
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "second");
        _second = Hash();
    }

    [Fact] void should_hash_the_physical_attachment() => _first.ShouldNotBeEmpty();
    [Fact] void should_refresh_when_only_the_attachment_changes() => _first.ShouldNotEqual(_second);
    [Fact] void should_not_change_the_workspace_revision() => Call("read-workspace", new { expectedRevision = _revision, view = "implementation-requirements" }).GetProperty("result").GetProperty("structuredContent").GetProperty("page").GetProperty("revision").GetString().ShouldEqual(_revision);

    [Fact]
    void should_reuse_analysis_when_attachment_is_unchanged()
    {
        var document = WorkspaceDocument.Create(
            "source",
            PortablePlayPath.Parse("application.play"),
            Encoding.UTF8.GetBytes(File.ReadAllText(Path.Combine(RootPath, "application.play"))));
        var workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        var root = new McpRoot(RootPath);
        var first = McpAttachmentContents.Refresh(root, workspace);
        var second = McpAttachmentContents.Refresh(root, first);
        ReferenceEquals(first, second).ShouldBeTrue();
        ReferenceEquals(McpWorkspaceAnalysis.For(first), McpWorkspaceAnalysis.For(second)).ShouldBeTrue();
    }

    [Fact]
    void should_keep_and_refresh_a_proposal_when_attachments_change()
    {
        var opened = Call("open-workspace").GetProperty("result").GetProperty("structuredContent");
        var revision = opened.GetProperty("revision").GetString();
        var catalog = opened.GetProperty("catalogRevision").GetString();
        var source = File.ReadAllText(Path.Combine(RootPath, "application.play"));
        File.WriteAllText(Path.Combine(RootPath, "Renamed.cs"), "renamed");
        var syntax = new ScreenplayCompiler().Compile(source.Replace("Handler.cs", "Renamed.cs", StringComparison.Ordinal)).Value!;
        var documentId = Call("read-workspace", new { expectedRevision = revision }).GetProperty("result")
            .GetProperty("structuredContent").GetProperty("page").GetProperty("items")[0].GetProperty("documentId").GetString();
        var proposal = Call("propose-ast", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = catalog,
            formatting = "CanonicalizeTouchedDocuments",
            documents = new[] { new { operation = "replace-document", documentId, node = SyntaxJson.Serialize(syntax) } }
        }).GetProperty("result").GetProperty("structuredContent");
        var id = proposal.GetProperty("proposalId").GetString();
        var proposed = Call("read-proposal", new { proposalId = id, view = "implementation-requirements" })
            .GetProperty("result").GetProperty("structuredContent");
        proposed.GetProperty("result").GetProperty("items")[0].GetProperty("attachmentResolution").GetString().ShouldEqual("Resolved");
        File.WriteAllText(Path.Combine(RootPath, "Handler.cs"), "changed again");
        _ = Hash();
        var retained = Call("read-proposal", new { proposalId = id, view = "implementation-requirements" })
            .GetProperty("result").GetProperty("structuredContent");
        retained.GetProperty("result").GetProperty("items")[0].GetProperty("contentHash").GetString().ShouldNotBeEmpty();
    }

    string Hash()
    {
        var page = Call("read-workspace", new { expectedRevision = _revision, view = "implementation-requirements" })
            .GetProperty("result").GetProperty("structuredContent").GetProperty("page");
        return page.GetProperty("items").EnumerateArray().Single().GetProperty("contentHash").GetString()!;
    }
}
