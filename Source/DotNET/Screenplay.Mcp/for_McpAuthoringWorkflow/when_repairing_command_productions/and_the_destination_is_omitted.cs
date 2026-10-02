// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_repairing_command_productions;

public class and_the_destination_is_omitted : given.a_producing_workspace
{
    ScreenplayWorkspace _candidate;
    JsonElement _diagnostic;

    void Establish()
    {
        OpenProducingWorkspace(true, DiagnosticCodes.OmittedProductionDestination);
        _diagnostic = Page("diagnostics", Opened.GetProperty("revision").GetString()!).EnumerateArray()
            .Single(diagnostic => diagnostic.GetProperty("code").GetString() == DiagnosticCodes.OmittedProductionDestination);
    }

    void Because()
    {
        Proposal = Result("propose-repair", Arguments());
        _candidate = Candidate(Proposal);
    }

    [Fact] void should_surface_an_information_diagnostic() => _diagnostic.GetProperty("severity").GetString().ShouldEqual("Information");
    [Fact] void should_expose_a_typed_replacement() => Repair.GetProperty("operations").EnumerateArray().Single().GetProperty("operation").GetString().ShouldEqual("replace");
    [Fact] void should_insert_the_explicit_destination() => _candidate.Documents[0].Text.ShouldContain("for projectId");
    [Fact] void should_preserve_the_comment() => _candidate.Documents[0].Text.ShouldContain("// preserve this comment");
    [Fact] void should_not_write_without_explicit_apply() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(Original).ShouldBeTrue();

    [Fact]
    void should_write_only_on_explicit_apply()
    {
        Apply(Opened, Proposal).GetProperty("success").GetBoolean().ShouldBeTrue();
        File.ReadAllText(Path.Combine(RootPath, "application.play")).ShouldContain("for projectId");
    }
}
