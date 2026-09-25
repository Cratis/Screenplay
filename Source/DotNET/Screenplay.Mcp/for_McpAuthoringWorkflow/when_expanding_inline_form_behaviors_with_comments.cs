// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_expanding_inline_form_behaviors_with_comments : given.an_authoring_connection
{
    JsonElement _proposal;
    string _source = string.Empty;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), "concept OrderId : Uuid\n");
        var module = Path.Combine(RootPath, "Shop");
        Directory.CreateDirectory(module);
        File.WriteAllText(Path.Combine(module, "Shop.play"), """
            module Shop
              form OrderForm for PlaceOrder
                field orderId label "Order"
                // submit action belongs inside the form
                on submit
                  execute PlaceOrder
                // click action belongs inside the form
                on click
                  navigate to Summary
            """);
        var feature = Path.Combine(module, "Ordering");
        Directory.CreateDirectory(feature);
        File.WriteAllText(Path.Combine(feature, "Ordering.play"), "module Shop\n  feature Ordering\n    slice StateView Summary\n");
        Initialize();
    }

    void Because()
    {
        var opened = Result("open-workspace", new { applicationName = "Shop" });
        _proposal = Result("expand-layout", new
        {
            expectedRevision = opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            layout = "feature",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        Apply(opened, _proposal);
        _source = File.ReadAllText(Path.Combine(RootPath, "Shop", "Shop.play"));
    }

    [Fact] void should_keep_both_behavior_comments_inside_the_form() => _source.ShouldContain("  form OrderForm for PlaceOrder\n    field orderId label \"Order\"\n    // submit action belongs inside the form\n    on submit\n      execute PlaceOrder\n    // click action belongs inside the form\n    on click\n      navigate to Summary");
    [Fact] void should_not_duplicate_behavior_comments() => _source.Split("// submit action belongs inside the form", StringSplitOptions.None).Length.ShouldEqual(2);
    [Fact] void should_not_report_dropped_comments() => _proposal.GetProperty("droppedCommentCount").GetInt32().ShouldEqual(0);
    [Fact] void should_not_report_play0288() => _proposal.GetRawText().ShouldNotContain("PLAY0288");
}
