// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_expanding_layout;

public class and_a_timeline_repair_precedes_expansion : given.an_authoring_connection
{
    JsonElement _opened;
    ScreenplayWorkspace _before = null!;
    ScreenplayWorkspace _repaired = null!;
    ScreenplayWorkspace _expanded = null!;

    void Establish()
    {
        File.WriteAllText(Path.Combine(RootPath, "application.play"), when_reading_timeline_repairs.Source);
        Initialize();
        _opened = Open();
        _before = Workspace();
        var repair = Page("repairs", _opened.GetProperty("revision").GetString()!).EnumerateArray().Single();
        var proposal = Result("propose-repair", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            diagnosticCode = "PLAY0516",
            subject = repair.GetProperty("subject"),
            formatting = "CanonicalizeTouchedDocuments"
        });
        _opened = Apply(_opened, proposal).GetProperty("workspace");
        _repaired = Workspace();
    }

    void Because()
    {
        var proposal = Result("expand-layout", new
        {
            expectedRevision = _opened.GetProperty("revision").GetString(),
            expectedCatalogRevision = _opened.GetProperty("catalogRevision").GetString(),
            layout = "slice",
            validation = "Authoring",
            formatting = "CanonicalizeTouchedDocuments"
        });
        _opened = Apply(_opened, proposal).GetProperty("workspace");
        _expanded = Workspace();
    }

    [Fact] void should_keep_the_repaired_sibling_order() => given.a_layout_order.Sequences(_expanded).ShouldEqual(given.a_layout_order.Sequences(_repaired));
    [Fact] void should_keep_the_same_executable_model_through_repair_and_expansion() => (WorkspaceRepairVerification.SameModel(_before, _repaired) && WorkspaceRepairVerification.SameModel(_repaired, _expanded)).ShouldBeTrue();
    [Fact] void should_keep_the_repair_free_of_later_producer_findings() => _expanded.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == "PLAY0516").ShouldBeEmpty();
    [Fact] void should_leave_catalog_revision_unchanged_by_the_order_only_repair() => _repaired.IdentityCatalog.Revision.ShouldEqual(_before.IdentityCatalog.Revision);
}
