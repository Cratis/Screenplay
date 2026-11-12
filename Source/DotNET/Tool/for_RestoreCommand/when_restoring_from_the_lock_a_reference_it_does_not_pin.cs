// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_RestoreCommand;

public class when_restoring_from_the_lock_a_reference_it_does_not_pin : for_ModelCheck.given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "application.play"), "system Mailer\n  extension \"cratis/mail\"\n");

    void Because() => _exitCode = ToolCommands.Run(["restore", Root, "--locked"], Output, Error);

    [Fact] void should_fail() => _exitCode.ShouldEqual(1);
    [Fact] void should_name_the_reference() => Error.ToString().ShouldContain("cratis/mail");
    [Fact] void should_not_write_a_lock() => File.Exists(Path.Combine(Root, "screenplay.lock")).ShouldBeFalse();
}
