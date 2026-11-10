// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_Agents;

public class when_reporting_unexposed_operations : given.an_agent_model
{
    [Fact]
    void should_be_silent_when_no_block_is_authored()
    {
        var result = Compiler.Compile(Model);
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnexposedAgentOperations);
        result.Value!.Agents.ShouldBeNull();
    }

    [Fact]
    void should_report_one_summary_and_warn_about_an_empty_allowlist()
    {
        var result = Compiler.Compile(Model + "\nagents\n  tokens personal");
        result.Success.ShouldBeTrue();
        result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnexposedAgentOperations).ShouldEqual(1);
        result.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.UnexposedAgentOperations).Message.ShouldContain("2 of 2");
        result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContain(DiagnosticCodes.AgentsExposeNothing);
    }

    [Fact]
    void should_not_report_success_information_for_an_invalid_allowlist()
    {
        var result = Compiler.Compile(Model + "\nagents");
        result.Diagnostics.ShouldNotContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnexposedAgentOperations);
    }
}
