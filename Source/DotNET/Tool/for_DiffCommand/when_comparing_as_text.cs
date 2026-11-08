// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_DiffCommand;

public class when_comparing_as_text : given.two_model_versions
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["diff", Root, After], Output, Error);

    [Fact] void should_succeed() => _exitCode.ShouldEqual(0);
    [Fact] void should_say_how_declarations_were_matched() => Output.ToString().ShouldContain("matched by address.");
    [Fact] void should_list_the_added_slice() => Output.ToString().ShouldContain("Added Slice Administration.Settings.Themes");
    [Fact] void should_list_the_removed_event() => Output.ToString().ShouldContain("Removed Event Projects.Registration.RegisterProject.ProjectRegistered");
    [Fact] void should_list_the_added_event() => Output.ToString().ShouldContain("Added Event Projects.Registration.RegisterProject.ProjectEnrolled");
    [Fact] void should_report_a_semantic_change() => Output.ToString().ShouldContain("Semantic change: yes.");
    [Fact] void should_report_nothing_on_the_error_stream() => Error.ToString().ShouldBeEmpty();
}
