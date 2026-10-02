// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow;

public class when_reading_optional_and_event_repairs : given.an_authoring_connection
{
    [Fact]
    void should_preserve_diagnostic_locations_and_propose_both_kinds_from_one_snapshot()
    {
        const string source = "module Projects\n  feature Naming\n    slice StateChange Rename\n      command Rename\n        projectId Uuid identifier\n        produces event Renamed\n          id \"Renamed\"\n          name String? = \"something\"\n";
        File.WriteAllText(Path.Combine(RootPath, "application.play"), source);
        Initialize();
        var opened = Open();
        var revision = opened.GetProperty("revision").GetString()!;
        var repairs = Page("repairs", revision).EnumerateArray().ToArray();
        repairs.Length.ShouldEqual(3);
        var eventRepair = repairs.Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0471");
        eventRepair.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(7);
        eventRepair.GetProperty("scope").GetString().ShouldEqual("occurrence");
        eventRepair.GetProperty("canFixAll").GetBoolean().ShouldBeTrue();
        eventRepair.GetProperty("retiredSemanticAddresses").GetArrayLength().ShouldEqual(0);
        var spelling = repairs.Single(value => value.GetProperty("diagnosticCode").GetString() == "PLAY0479" && value.GetProperty("scope").GetString() == "occurrence");
        spelling.GetProperty("location").GetProperty("line").GetInt32().ShouldEqual(8);
        repairs.Single(value => value.GetProperty("scope").GetString() == "document").GetProperty("operations").GetArrayLength().ShouldEqual(1);

        foreach (var repair in repairs)
        {
            var proposal = Result("propose-repair", new
            {
                expectedRevision = revision,
                expectedCatalogRevision = opened.GetProperty("catalogRevision").GetString(),
                diagnosticCode = repair.GetProperty("diagnosticCode").GetString(),
                subject = repair.GetProperty("subject"),
                formatting = repair.GetProperty("requiredFormatting").GetString()
            });
            var candidate = Candidate(proposal).Documents.Single().Text;
            candidate.ShouldContain("name String optional = \"something\"");
            if (repair.GetProperty("diagnosticCode").GetString() == "PLAY0471")
            {
                candidate.ShouldNotContain("id \"Renamed\"");
            }
            else
            {
                candidate.ShouldContain("id \"Renamed\"");
            }
        }

        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldEqual(source);
    }
}
