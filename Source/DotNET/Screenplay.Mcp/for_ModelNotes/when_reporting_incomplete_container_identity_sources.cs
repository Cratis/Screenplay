// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_ModelNotes;

public class when_reporting_incomplete_container_identity_sources : given.a_split_notes_workspace
{
    [Theory]
    [InlineData(false, "unparsed")]
    [InlineData(true, "unplaced")]
    public void should_report_the_incomplete_document_and_refuse_new_identities(bool unplaced, string reason)
    {
        OpenSplit(unbound: true);
        if (unplaced)
        {
            File.AppendAllText(Path.Combine(RootPath, "Orders/Orders.play"), "\n  feature Another\n    import \"PlaceOrder.play\"\n");
        }
        else
        {
            File.AppendAllText(Path.Combine(RootPath, "application.play"), "\nnot valid Screenplay\n");
        }

        Opened = Result(Workspaces.Open(Arguments(new { applicationName = "Acme" })));
        Split = ScreenplayWorkspace.Create("Acme", Root.Read(), SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Acme")));
        Index = WorkspaceSyntaxIndex.Create(Split);
        Split.IdentityCatalog.Semantics.ShouldBeEmpty();
        var report = Opened.GetProperty("containerIdentities");
        report.GetProperty("complete").GetBoolean().ShouldBeFalse();
        report.GetProperty("documents").EnumerateArray().Any(document => document.GetProperty("reason").GetString() == reason).ShouldBeTrue();
        report.GetProperty("note").GetString().ShouldContain("No new identities");
        Result(Workspaces.State(McpJson.Empty)).GetProperty("containerIdentities").GetRawText().ShouldEqual(report.GetRawText());
        foreach (var target in Targets(SemanticKind.Module))
        {
            Catch.Exception(() => ProposeNote(target)).Message.ShouldContain("incomplete");
        }
    }
}
