// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Comparison;

namespace Cratis.Screenplay.Tool.for_DiffCommand;

public class when_comparing_agent_exposure : given.two_model_versions
{
    const string Agents = "\nagents\n  tokens personal\n  query GetProjects\n    scope read\n    description \"List projects\"\n  command RegisterProject\n    scope write\n    description \"Register projects\"";
    JsonElement _exposure;
    AgentExposureDifference _library;
    int _exit;
    string _text;

    void Establish() => Write(After, Source + Agents);

    void Because()
    {
        _exit = ToolCommands.Run(["diff", Root, After, "--format", "json"], Output, Error);
        _exposure = JsonElement.Parse(Output.ToString()).GetProperty("difference").GetProperty("agentExposure");
        _library = ModelComparison.Compare(ComparedModel.WithoutIdentities(ModelSource.Load(Root).Workspace), ComparedModel.WithoutIdentities(ModelSource.Load(After).Workspace)).AgentExposure;
        Output.GetStringBuilder().Clear();
        ToolCommands.Run(["diff", Root, After], Output, Error);
        _text = Output.ToString();
    }

    [Fact] void should_preserve_exit_behavior() => _exit.ShouldEqual(0);
    [Fact] void should_report_library_verdict() => _exposure.GetProperty("hasExposureChange").GetBoolean().ShouldEqual(_library.HasExposureChange!.Value);
    [Fact] void should_report_library_matching() => _exposure.GetProperty("matching").GetString().ShouldEqual("address");
    [Fact] void should_report_every_library_finding() => _exposure.GetProperty("changes").GetArrayLength().ShouldEqual(_library.Changes.Count);
    [Fact] void should_print_the_newly_exposed_query() => _text.ShouldContain("Added Query Projects.Registration.List.GetProjects");
    [Fact] void should_print_the_newly_exposed_command() => _text.ShouldContain("Added Command Projects.Registration.RegisterProject.RegisterProject");
    [Fact] void should_print_separate_exposure_verdict() => _text.ShouldContain("Agent exposure change: yes.");
    [Fact] void should_print_the_boundary() => _text.ShouldContain("not effective authorization");

    [Fact]
    void should_print_unknown_instead_of_a_clean_bill_for_invalid_compilation()
    {
        Write(After, Source + Agents.Replace("query GetProjects", "query Missing", StringComparison.Ordinal));
        Output.GetStringBuilder().Clear();
        ToolCommands.Run(["diff", Root, After], Output, Error).ShouldEqual(0);
        Output.ToString().ShouldContain("Agent exposure change: unknown; before Available, after CompilationFailed.");
        Output.ToString().ShouldNotContain("Added Query");
    }
}
