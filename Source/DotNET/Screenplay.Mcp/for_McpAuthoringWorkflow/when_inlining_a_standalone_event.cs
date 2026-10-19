// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_inlining_a_standalone_event : given.an_authoring_connection
{
    const string Eligible = "module Projects\n  feature Naming\n    slice StateChange Rename\n      event Renamed // preserve this\n        name String\n      command Rename\n        projectId Uuid identifier\n        name String\n        produces Renamed\n          for projectId\n          name = name\n";

    [Fact]
    void should_expose_an_explicit_preview_that_changes_nothing_until_apply()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Eligible);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var proposal = Result("propose-inline-event", Arguments(opened, revision, "CanonicalizeTouchedDocuments"));
        var candidate = Candidate(proposal);
        WorkspaceSyntaxIndex.Create(candidate).Entries.Single(value => value.Node is EventSyntax).Member.ShouldEqual("inlineEvent");
        candidate.Documents[0].Text.ShouldContain("// preserve this");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(Eligible);
        Apply(opened, proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
    }

    [Fact]
    void should_refuse_an_unprovable_inlining_and_leave_the_source_unchanged()
    {
        var source = Eligible.Replace("          for projectId\n", string.Empty, StringComparison.Ordinal);
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var response = Call("propose-inline-event", Arguments(opened, opened.GetProperty("revision").GetString()!, "CanonicalizeTouchedDocuments")).GetProperty("result");
        response.GetProperty("structuredContent").GetProperty("conflicts")[0].GetProperty("message").GetString()!.ShouldContain("omits 'for'");
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }

    object Arguments(System.Text.Json.JsonElement opened, string revision, string formatting) => new
    {
        expectedRevision = revision,
        expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
        subject = Node("EventSyntax", revision).GetProperty("handle"),
        formatting
    };
}
