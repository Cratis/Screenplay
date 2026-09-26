// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_a_template_with_fits_slot_comments : given.an_authoring_connection
{
    const string Source = """
        layout Main
          content
        module Shop
          screen template Shell
            // shell goes in the content region
            fits slot content // target slot
            main
          feature Ordering
            slice StateView Orders
        """;

    JsonElement _proposal;
    JsonElement _dropped;
    string _moduleSource = string.Empty;

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
    }

    [Fact] void should_keep_comments_on_the_fits_slot_directive() => _moduleSource.ShouldContain("  screen template Shell\n    // shell goes in the content region\n    fits slot content // target slot");
    [Fact] void should_report_no_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_dropped_comments() => _dropped.GetArrayLength().ShouldEqual(0);
}
