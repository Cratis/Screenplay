// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_proposing_an_event_rename_pin : given.an_authoring_connection
{
    [Theory]
    [InlineData(null, false, false, "Renamed")]
    [InlineData(null, true, false, null)]
    [InlineData("Original", true, false, "Original")]
    [InlineData("Again", false, false, null)]
    [InlineData(null, false, true, "Renamed")]
    void should_report_added_or_retained_pins_even_when_binding_is_unavailable(string? existingPin, bool neverPersisted, bool unsupported, string? expectedPin)
    {
        var source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n";
        if (existingPin is not null) source += $"          id \"{existingPin}\"\n";
        if (unsupported) source = "numbers exact\n" + source;
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var proposal = Result("propose-rename", new
        {
            expectedRevision = revision,
            expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
            target = Node("EventSyntax", revision).GetProperty("handle"),
            expectedName = "Renamed",
            newName = "Again",
            eventNeverPersisted = neverPersisted
        });
        var warnings = proposal.GetProperty("eventPinWarnings");
        warnings.GetArrayLength().ShouldEqual(expectedPin is null ? 0 : 1);
        if (expectedPin is not null)
        {
            warnings[0].GetProperty("code").GetString().ShouldEqual("PLAY0686");
            warnings[0].GetProperty("severity").GetString().ShouldEqual("Warning");
            warnings[0].GetProperty("message").GetString().ShouldEqual($"Event 'Again' id pin '{expectedPin}' is not yet carried to renderers; generated code uses the new name 'Again'.");
        }

        var reviewed = Result("read-proposal", new { proposalId = proposal.GetProperty("proposalId").GetString() });
        reviewed.GetProperty("eventPinWarnings").GetRawText().ShouldEqual(warnings.GetRawText());
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }
}
