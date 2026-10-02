// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.when_repairing_command_productions;

public class and_the_event_is_undeclared : given.a_producing_workspace
{
    ScreenplayWorkspace _candidate;

    void Establish() => OpenProducingWorkspace(false, DiagnosticCodes.UnknownEvent);

    void Because()
    {
        Proposal = Result("propose-repair", Arguments());
        _candidate = Candidate(Proposal);
    }

    [Fact] void should_expose_a_typed_addition() => Repair.GetProperty("operations").EnumerateArray().Single().GetProperty("operation").GetString().ShouldEqual("add");
    [Fact] void should_declare_the_event() => _candidate.Documents[0].Text.ShouldContain("event ProjectRegistered");
    [Fact] void should_resolve_the_diagnostic() => _candidate.Compilation.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnknownEvent || diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding).ShouldBeFalse();
    [Fact] void should_preserve_the_comment() => _candidate.Documents[0].Text.ShouldContain("// preserve this comment");
    [Fact] void should_not_write_without_explicit_apply() => File.ReadAllBytes(Path.Combine(RootPath, "application.play")).SequenceEqual(Original).ShouldBeTrue();
}
