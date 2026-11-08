// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_RenameCommand;

public class when_renaming_with_another_kind : Tool.given.a_renderable_model
{
    int _exitCode;

    void Because() => _exitCode = ToolCommands.Run(["rename", "Projects.Registration.RegisterProject.ProjectRegistered", "Other", Root, "--kind", "Command"], Output, Error);

    [Fact] void should_not_run() => _exitCode.ShouldEqual(2);
    [Fact] void should_name_the_kind() => Error.ToString().ShouldContain("of kind Command");
    [Fact] void should_leave_the_source_alone() => File.ReadAllText(Path.Combine(Root, "application.play")).ShouldEqual(Source);
}
