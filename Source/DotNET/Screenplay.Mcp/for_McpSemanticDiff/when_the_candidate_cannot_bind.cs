// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_the_candidate_cannot_bind : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        Workspace = Create(source);
        Propose("system Store\n" + source + "        extra String\n      operation StoreProject\n        uses Store\n        name String\n");
        Diff = Read();
    }

    [Fact] void should_fall_back_to_the_authored_event_shape() => Items("events").Single().GetProperty("member").GetString().ShouldEqual("extra");
    [Fact] void should_report_executable_unavailability() => Diff.GetProperty("executableAfterAvailable").GetBoolean().ShouldBeFalse();
    [Fact] void should_not_hide_the_unknown_identity_comparison() => Diff.GetProperty("sections").EnumerateArray().Single(section => section.GetProperty("section").GetString() == "identities").GetProperty("complete").GetBoolean().ShouldBeFalse();
}
