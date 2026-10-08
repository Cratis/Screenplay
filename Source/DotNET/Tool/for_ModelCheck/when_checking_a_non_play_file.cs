// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Tool.for_ModelCheck;

public class when_checking_a_non_play_file : given.a_model
{
    int _exitCode;

    void Establish() => File.WriteAllText(Path.Combine(Root, "source.txt"), "module M");
    void Because() => _exitCode = ModelCheck.Run([Path.Combine(Root, "source.txt")], Output, Error);

    [Fact] void should_report_a_usage_error() => _exitCode.ShouldEqual(2);
    [Fact] void should_explain_the_required_file_type() => Error.ToString().ShouldContain("is not a .play file");
}
