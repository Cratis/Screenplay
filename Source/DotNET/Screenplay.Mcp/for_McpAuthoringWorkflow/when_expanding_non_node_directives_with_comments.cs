// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_non_node_directives_with_comments : given.an_authoring_connection
{
    const string Source = """
        policy Member
          require authenticated
        persona Clerk
          policy Member // policy note
          description "A clerk" // persona note
        module Shop
          feature Orders
            slice StateChange Place
              event OrderPlaced
                id String // property note
              constraint UniqueOrder // header note
                message "Already placed" // message note
                released by OrderRemoved // release note
                ignore casing // casing note
                unique id on OrderPlaced // rule note
            slice StateView Board
              query List => Order[]
                scoped to identity // scope note
                by id String // by note
        """;

    JsonElement _proposal;
    JsonElement _dropped;
    string _moduleSource = string.Empty;
    string _rootSource = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), Source);
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Shop" });
        _proposal = Result("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "module",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _dropped = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items");
        Apply(opened, _proposal);
        _moduleSource = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
        _rootSource = File.ReadAllText(Path.Combine(RootPath, "application.play"));
    }

    [Fact] void should_keep_persona_description_on_its_line() => _rootSource.ShouldContain("description \"A clerk\" // persona note\n  policy Member // policy note");
    [Fact] void should_keep_constraint_comments_on_their_lines() => _moduleSource.ShouldContain("id String // property note\n\n      constraint UniqueOrder // header note\n        unique id on OrderPlaced // rule note\n        released by OrderRemoved // release note\n        ignore casing // casing note\n        message \"Already placed\" // message note");
    [Fact] void should_keep_query_comments_on_their_lines() => _moduleSource.ShouldContain("by id String // by note\n        scoped to identity // scope note");
    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);
    [Fact] void should_not_report_play0288() => _proposal.GetRawText().ShouldNotContain("PLAY0288");
}
