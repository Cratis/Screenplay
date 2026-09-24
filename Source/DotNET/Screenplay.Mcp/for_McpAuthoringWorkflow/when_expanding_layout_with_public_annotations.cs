// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_layout_with_public_annotations : given.an_order_model_with_comments
{
    JsonElement _proposal;
    JsonElement _dropped;
    string _moduleSource = string.Empty;

    void Because()
    {
        Opened = Result("open-workspace", new { applicationName = "Repro" });
        _proposal = Result("expand-layout", new
        {
            expectedRevision = Opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = Opened.GetProperty("catalogRevision").GetString(),
            layout = "module",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _dropped = Result("read-proposal", new { proposalId = _proposal.GetProperty("proposalId").GetString(), view = "dropped-comments" }).GetProperty("result").GetProperty("items");
        Apply(Opened, _proposal);
        _moduleSource = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
    }

    [Fact] void should_keep_public_annotation_in_the_new_layout() => _moduleSource.ShouldContain("// @public command PlaceOrder");
    [Fact] void should_keep_owner_annotation_in_the_new_layout() => _moduleSource.ShouldContain("// @owner sales");
    [Fact] void should_report_no_dropped_comments_across_documents() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_list_no_lost_annotations() => _dropped.GetArrayLength().ShouldEqual(0);
}
